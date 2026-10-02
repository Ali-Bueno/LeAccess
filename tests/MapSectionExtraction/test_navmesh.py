"""Offline checks for the actual navigation decoder and conservative clipping helper."""
from pathlib import Path
import struct
import sys
from types import SimpleNamespace
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[2] / "tools"))
from map_section_navmesh import NavigationCell, NavigationFootprints, load_navigation, read_tile
from shapely.geometry import Polygon, box
from shapely.ops import unary_union


def tile(vertices=None, polygons=None):
    vertices = vertices or [(0, 0, 0), (4, 0, 0), (0, 0, 4)]
    polygons = polygons or [([0, 1, 2], [0, 0, 0], 1)]
    header = bytearray(72)
    header[:4] = b"VAND"
    struct.pack_into("<I", header, 4, 16)
    struct.pack_into("<II", header, 20, len(polygons), len(vertices))
    raw = header + b"".join(struct.pack("<fff", *v) for v in vertices)
    for indices, neighbours, mask in polygons:
        raw += struct.pack("<6H6HII", *(indices + [0] * (6 - len(indices))),
                           *(neighbours + [0] * (6 - len(neighbours))), mask, len(indices))
    return raw


def dataset(raw=None):
    return {"m_AgentTypeID": 0, "m_Position": dict(x=0, y=0, z=0),
            "m_Rotation": dict(x=0, y=0, z=0, w=1),
            "m_NavMeshTiles": [{"m_MeshData": list(raw or tile())}],
            "m_NavMeshBuildSettings": {"agentClimb": .4}}


def environment(*datasets):
    objects = [SimpleNamespace(type=SimpleNamespace(name="NavMeshData"), read_typetree=lambda d=d: d)
               for d in datasets]
    return SimpleNamespace(objects=objects)


def geometry(parts):
    return unary_union([Polygon([(p["x"], p["z"]) for p in part["polygon"]]) for part in parts])


class NavigationDecoderTests(unittest.TestCase):
    def test_known_triangle_and_height(self):
        cells = read_tile(tile([(0, 1, 0), (4, 2, 0), (0, 3, 4)]))
        self.assertEqual(len(cells), 1)
        self.assertEqual(cells[0].footprint.area, 8)
        self.assertEqual((cells[0].min_y, cells[0].max_y), (1, 3))

    def test_layout_magic_version_and_truncation(self):
        raw = tile()
        invalid = [raw[:71], b"BAD!" + raw[4:], raw[:4] + struct.pack("<I", 15) + raw[8:], raw[:-1]]
        for data in invalid:
            with self.subTest(size=len(data)), self.assertRaises(ValueError):
                read_tile(data)

    def test_zero_and_overflow_counts(self):
        for offset, count in [(20, 0), (24, 0), (20, 0xFFFFFFFF), (24, 0xFFFFFFFF)]:
            raw = tile()
            struct.pack_into("<I", raw, offset, count)
            with self.subTest(offset=offset, count=count), self.assertRaises(ValueError):
                read_tile(raw)

    def test_polygon_count_and_vertex_indices(self):
        for indices in [[0, 1], [0, 1, 3]]:
            with self.subTest(indices=indices), self.assertRaises(ValueError):
                read_tile(tile(polygons=[(indices, [0] * len(indices), 1)]))
        raw = tile()
        struct.pack_into("<I", raw, len(raw) - 4, 7)
        with self.assertRaises(ValueError):
            read_tile(raw)

    def test_neighbour_bounds_and_external_links(self):
        with self.assertRaises(ValueError):
            read_tile(tile(polygons=[([0, 1, 2], [2, 0, 0], 1)]))
        self.assertEqual(len(read_tile(tile(polygons=[([0, 1, 2], [0x8000, 0, 0], 1)]))), 1)
        self.assertEqual(len(read_tile(tile(polygons=[([0, 1, 2], [0, 0, 0], 1)]))), 1)

    def test_nonwalkable_areas_are_not_silently_promoted(self):
        self.assertEqual(read_tile(tile(polygons=[([0, 1, 2], [0, 0, 0], 2)])), [])
        self.assertEqual(read_tile(tile(polygons=[([0, 1, 2], [0, 0, 0], 3)])), [])

    def test_nonfinite_vertices(self):
        for invalid in [float("nan"), float("inf"), -float("inf")]:
            with self.subTest(value=invalid), self.assertRaises(ValueError):
                read_tile(tile([(0, invalid, 0), (4, 0, 0), (0, 0, 4)]))

    def test_degenerate_and_crossing_footprints(self):
        for vertices in [[(0, 0, 0), (1, 0, 1), (2, 0, 2)],
                         [(0, 0, 0), (4, 0, 4), (0, 0, 4), (4, 0, 0)]]:
            indices = list(range(len(vertices)))
            with self.subTest(vertices=vertices), self.assertRaises(ValueError):
                read_tile(tile(vertices, [(indices, [0] * len(indices), 1)]))

    def test_duplicate_indices_are_rejected_before_catalog_generation(self):
        vertices = [(0, 0, 0), (4, 0, 0), (4, 0, 4), (0, 0, 4)]
        with self.assertRaises(ValueError):
            read_tile(tile(vertices, [([0, 1, 2, 2, 3], [0] * 5, 1)]))

    def test_duplicate_projected_points_are_rejected(self):
        vertices = [(0, 0, 0), (4, 0, 0), (4, 0, 4), (4, 1, 4), (0, 0, 4)]
        with self.assertRaises(ValueError):
            read_tile(tile(vertices, [([0, 1, 2, 3, 4], [0] * 5, 1)]))


class FootprintClippingTests(unittest.TestCase):
    def test_disjoint_components_do_not_fill_gap(self):
        nav = NavigationFootprints([NavigationCell(box(0, 0, 1, 1), 0, 0),
                                    NavigationCell(box(3, 0, 4, 1), 0, 0)], .4)
        parts = nav.clip(box(0, 0, 4, 1), 0, 0)
        self.assertEqual(len(parts), 2)
        self.assertEqual(geometry(parts).area, 2)
        self.assertFalse(geometry(parts).contains(box(1.5, .25, 2.5, .75)))
        self.assertTrue(all(len(p["polygon"]) == 4 for p in parts))

    def test_exact_intersection_not_structural_hull(self):
        triangle = Polygon([(0, 0), (4, 0), (0, 4)])
        nav = NavigationFootprints([NavigationCell(triangle, 0, 0)], .4)
        parts = nav.clip(box(1, 1, 5, 5), 0, 0)
        self.assertTrue(geometry(parts).equals(triangle.intersection(box(1, 1, 5, 5))))
        self.assertEqual(geometry(parts).area, 2)

    def test_adjacent_nav_cells_preserve_shared_seam(self):
        nav = NavigationFootprints([NavigationCell(box(0, 0, 2, 2), 0, 0),
                                    NavigationCell(box(2, 0, 4, 2), 0, 0)], .4)
        parts = nav.clip(box(1, 0, 3, 2), 0, 0)
        self.assertEqual(len(parts), 2)
        self.assertTrue(geometry(parts).equals(box(1, 0, 3, 2)))

    def test_holes_require_subdivision_instead_of_repair(self):
        hole = Polygon([(0, 0), (4, 0), (4, 4), (0, 4)],
                       holes=[[(1, 1), (3, 1), (3, 3), (1, 3)]])
        nav = NavigationFootprints([NavigationCell(box(0, 0, 4, 4), 0, 0)], .4)
        with self.assertRaisesRegex(ValueError, "manual subdivision"):
            nav.clip(hole, 0, 0)

    def test_different_floors_not_combined(self):
        nav = NavigationFootprints([NavigationCell(box(0, 0, 4, 4), 0, 0),
                                    NavigationCell(box(0, 0, 4, 4), 10, 10)], .4)
        low = nav.clip(box(1, 1, 3, 3), 0, 0)
        high = nav.clip(box(1, 1, 3, 3), 10, 10)
        self.assertEqual(len(low), 1)
        self.assertEqual((low[0]["minY"], low[0]["maxY"]), (-.4, .4))
        self.assertEqual(len(high), 1)
        self.assertEqual((high[0]["minY"], high[0]["maxY"]), (9.6, 10.4))

    def test_authored_climb_height_boundary(self):
        nav = NavigationFootprints([NavigationCell(box(0, 0, 4, 4), 2, 3)], .5)
        self.assertEqual(len(nav.clip(box(1, 1, 2, 2), 1.5, 1.5)), 1)
        self.assertEqual(nav.clip(box(1, 1, 2, 2), 1.49, 1.49), [])

    def test_touching_without_area_not_emitted(self):
        nav = NavigationFootprints([NavigationCell(box(0, 0, 1, 1), 0, 0)], .4)
        self.assertEqual(nav.clip(box(1, 0, 2, 1), 0, 0), [])
        self.assertEqual(nav.clip(box(1, 1, 2, 2), 0, 0), [])

    def test_invalid_geometry_height_and_climb_rejected(self):
        cell = NavigationCell(box(0, 0, 4, 4), 0, 0)
        for climb in [0, -1, float("nan"), float("inf")]:
            with self.subTest(climb=climb), self.assertRaises(ValueError):
                NavigationFootprints([cell], climb)
        with self.assertRaises(ValueError):
            NavigationFootprints([], .4)
        nav = NavigationFootprints([cell], .4)
        for heights in [(1, 0), (float("nan"), 1), (0, float("inf"))]:
            with self.subTest(heights=heights), self.assertRaises(ValueError):
                nav.clip(box(0, 0, 1, 1), *heights)
        with self.assertRaises(ValueError):
            nav.clip(Polygon([(0, 0), (4, 4), (0, 4), (4, 0)]), 0, 0)


class BundleSelectionTests(unittest.TestCase):
    def test_default_agent_selected_and_identity_transform_required(self):
        other = dataset(); other["m_AgentTypeID"] = 7
        with patch("map_section_navmesh.UnityPy.load", return_value=environment(dataset(), other)):
            self.assertEqual(len(load_navigation("unused.bundle").cells), 1)
        for field, value in [("m_Position", dict(x=1, y=0, z=0)),
                             ("m_Rotation", dict(x=0, y=1, z=0, w=0))]:
            data = dataset(); data[field] = value
            with self.subTest(field=field), patch("map_section_navmesh.UnityPy.load", return_value=environment(data)), self.assertRaises(ValueError):
                load_navigation("unused.bundle")

    def test_ambiguous_or_missing_default_dataset_rejected(self):
        for env in [environment(), environment(dataset(), dataset())]:
            with patch("map_section_navmesh.UnityPy.load", return_value=env), self.assertRaises(ValueError):
                load_navigation("unused.bundle")


if __name__ == "__main__":
    unittest.main()
