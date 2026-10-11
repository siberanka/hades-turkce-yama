import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'tools'))
from context import localized_records, objects


class ContextTests(unittest.TestCase):
    def test_notes_belong_to_exact_object_and_repeated_ids_are_retained(self):
        text = '''{ /* Event: NotInherited */ Texts = [
            { /* Event: First\n Speaker: A {braced} sentence. */
              Id="Cue_1" Speaker="Speaker" DisplayName="Une phrase." }
            { /* Event: Later */ Id="Cue_1" Speaker="Other" DisplayName="Autre." }
            { Id="Plain" DisplayName="Menu" } ] }'''
        records = list(localized_records(text))
        self.assertEqual([r['events'] for r in records], [['First'], ['Later'], []])
        self.assertEqual([r['id'] for r in records], ['Cue_1', 'Cue_1', 'Plain'])
        self.assertEqual(records[1]['speaker'], 'Other')

    def test_nested_lua_names_conditions_and_quoted_braces(self):
        text = '''Root = { ["Scene"] = { RequiredTextLines = { "Before" },
          { Cue = "/VO/Test_1", Text = "a } brace", Args = { Text="not direct" } },
          -- { Cue="fake" }
        } }'''
        nodes = list(objects(text))
        cue = next(n for n in nodes if 'Cue' in n['fields'])
        self.assertEqual(cue['parents'], ['Root', 'Scene'])
        self.assertEqual(cue['fields']['Text'], 'a } brace')
        event = next(n for n in nodes if n['name'] == 'Scene')
        self.assertIn('RequiredTextLines', text[event['start']:event['end']])
        self.assertEqual(len([n for n in nodes if 'Cue' in n['fields']]), 1)

    def test_malformed_objects_fail_closed(self):
        for text in ('Root={', '}'):
            with self.assertRaises(ValueError):
                list(objects(text))
