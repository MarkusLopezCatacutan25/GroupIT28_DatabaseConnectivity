from pathlib import Path
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from dev01_check import metadata_errors, opaque


class MetadataChecks(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.root = Path(self.tmp.name)

    def tearDown(self):
        self.tmp.cleanup()

    def write(self, name, text=""):
        path = self.root / name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text)
        return name

    def test_missing_metadata_is_failure(self):
        name = self.write("SavorPinoyPrototype/Assets/Hello.cs")
        self.assertTrue(any("Missing" in e for e in metadata_errors(self.root, [name])))

    def test_valid_metadata(self):
        name = self.write("SavorPinoyPrototype/Assets/Hello.cs")
        meta = self.write(name + ".meta", "guid: " + "a" * 32 + "\n")
        self.assertEqual([], metadata_errors(self.root, [name, meta]))

    def test_duplicate_guid_is_failure(self):
        files = []
        for name in ["Hello.cs", "World.cs"]:
            path = self.write("SavorPinoyPrototype/Assets/" + name)
            files += [path, self.write(path + ".meta", "guid: " + "a" * 32 + "\n")]
        self.assertTrue(any("Duplicate" in e for e in metadata_errors(self.root, files)))

    def test_orphan_metadata_is_failure(self):
        path = self.write("SavorPinoyPrototype/Assets/Missing.cs.meta", "guid: " + "a" * 32 + "\n")
        self.assertTrue(any("Orphan" in e for e in metadata_errors(self.root, [path])))

    def test_native_plugin_contents_are_opaque(self):
        self.assertTrue(opaque("SavorPinoyPrototype/Assets/Plugins/Firebase.androidlib/res/values/google-services.xml"))
        self.assertFalse(opaque("SavorPinoyPrototype/Assets/Plugins/Firebase.androidlib"))
        self.assertFalse(opaque("SavorPinoyPrototype/Assets/Sprites/photo.png"))

    def test_invalid_guid_is_failure(self):
        name = self.write("SavorPinoyPrototype/Assets/Hello.cs")
        meta = self.write(name + ".meta", "guid: invalid\n")
        self.assertTrue(any("Invalid" in e for e in metadata_errors(self.root, [name, meta])))

    def test_gitkeep_is_not_a_unity_asset(self):
        name = self.write("SavorPinoyPrototype/Assets/.gitkeep")
        self.assertEqual([], metadata_errors(self.root, [name]))


if __name__ == "__main__":
    unittest.main()
