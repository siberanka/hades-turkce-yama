import csv
import io
import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'tools'))
from catalog import csv_strings, sjson_strings, encode, digest, entry_scope
from validate import validate


class CatalogTests(unittest.TestCase):
    def test_csv_header_variants_multiline_quotes_and_suffix(self):
        for blanks in (2, 3):
            columns = ['Speaker', 'ID', 'ID', 'ID'] + [''] * blanks + ['Line']
            values = ['Recorded', 'Voice_', '001', 'Voice_001a'] + [''] * blanks + ['Hello, "friend"\nNext line']
            buffer = io.StringIO(newline='')
            writer = csv.writer(buffer)
            writer.writerows([columns, values])
            text = buffer.getvalue()
            entries = list(csv_strings(text))
            self.assertEqual(len(entries), 1)
            self.assertEqual(entries[0][3], values[-1])
            _, a, b, _, kind = entries[0]
            patched = text[:a] + encode('Merhaba, "dostum"\nİkinci satır', kind).decode() + text[b:]
            rows = list(csv.reader(io.StringIO(patched)))
            self.assertEqual(rows[1][:-1], values[:-1])
            self.assertEqual(rows[1][-1], 'Merhaba, "dostum"\nİkinci satır')

    def test_comment_braces_do_not_create_entries(self):
        text = '/* { Id="fake" DisplayName="fake" } */ { Texts = [ { Id="real" DisplayName="Hello {#Format}" Description="World" } ] }'
        result = list(sjson_strings(text))
        self.assertEqual(len(result), 2)
        self.assertTrue(all(row[0].startswith('real::') for row in result))

    def test_lua_nested_context_and_comments(self):
        text = '-- { Cue="fake" Text="fake" }\nRoot={ { Cue="/VO/A_001", Args={ Text="skip" }, Text="Hello" }, --[[ { } ]] }'
        result = list(sjson_strings(text, lua=True))
        self.assertEqual([(r[0], r[3]) for r in result], [('/VO/A_001::Text::1', 'Hello')])

    def test_literals_roundtrip(self):
        for source in ['@GUI\\Shell\\EasyMode Hello', 'line\nsecond', 'A "quote"', 'İı Şş Ğğ Çç Öö Üü']:
            text = '{ Id="x" DisplayName=' + encode(source, 'sjson').decode() + ' }'
            self.assertEqual(list(sjson_strings(text))[0][3], source)

    def test_duplicate_ids_get_distinct_occurrences(self):
        result = list(sjson_strings('{ Id="x" DisplayName="one" } { Id="x" DisplayName="two" }'))
        self.assertEqual([r[0] for r in result], ['x::DisplayName::1', 'x::DisplayName::2'])

    def test_sjson_wire_directives_are_not_json_escaped(self):
        target = r'{#ChoiceFormat}\[ Hasar: \Column 380 {$Value} \]'
        self.assertEqual(encode(target, 'sjson'), ('"' + target + '"').encode())
        self.assertNotIn(b'\\\\Column', encode(target, 'sjson'))
        self.assertEqual(encode('Bir "söz"', 'sjson'), '"""Bir "söz""""'.encode())

    def test_raw_credits_do_not_hide_following_entries(self):
        text = '{ Id="credits" DisplayName="""A "quoted" credit"""" } { Id="next" DisplayName="Save" }'
        rows = list(sjson_strings(text))
        self.assertEqual([(r[0], r[3]) for r in rows],
                         [('credits::DisplayName::1', 'A "quoted" credit"'), ('next::DisplayName::1', 'Save')])

    def test_generated_visible_titles_are_translated_and_credits_are_protected(self):
        self.assertEqual(entry_scope('MacroText.sjson', 'UpgradeChoiceMenu_Title_ZeusUpgrade::DisplayName::1', 'Boons of Zeus'), 'game')
        self.assertEqual(entry_scope('HelpText.en.sjson', 'AboutScreen_Credits::Description::1', 'Original credits'), 'protected')
        self.assertEqual(entry_scope('HelpText.en.sjson', 'InGameCredits_Developer::DisplayName::1', 'A developer'), 'protected')
        self.assertEqual(entry_scope('HelpText.en.sjson', 'MainMenuScreen_PatchNotes_0056::Description::1', 'Old patch notes'), 'historical')
        self.assertEqual(entry_scope('HelpText.en.sjson', 'CreditsButton::DisplayName::1', 'Credits'), 'game')

    def test_validation_rejects_stale_source_and_missing_markers(self):
        entry = {'key': 'x', 'source': 'Hello {$Value}', 'source_sha256': digest(b'Hello {$Value}'), 'scope': 'game'}
        row = {'key': 'x', 'target': 'Merhaba', 'source_sha256': 'stale', 'stage': 'draft'}
        report = validate([entry], [row])
        self.assertEqual(len(report['errors']), 2)

    def test_validation_allows_reordering_variables(self):
        entry = {'key': 'x', 'source': '{$A} from {$B}', 'source_sha256': 'hash', 'scope': 'game'}
        row = {'key': 'x', 'target': '{$B} içinden {$A}', 'source_sha256': 'hash', 'stage': 'draft'}
        self.assertEqual(validate([entry], [row])['errors'], [])

    def test_review_is_invalidated_by_text_edit_and_requires_game_evidence(self):
        entry = {'key': 'x', 'source': 'Hello', 'source_sha256': digest(b'Hello'), 'scope': 'game'}
        row = {'key': 'x', 'target': 'Merhaba', 'source_sha256': entry['source_sha256'], 'stage': 'context-reviewed'}
        evidence = {'x': {'source_sha256': entry['source_sha256'], 'target_sha256': digest(b'Merhaba'),
                         'references': ['synthetic scene'], 'meaning': 'greeting', 'voice': 'neutral',
                         'scene': 'first meeting', 'formatting': 'no markers'}}
        self.assertEqual(validate([entry], [row], evidence)['errors'], [])
        row['target'] = 'Selam'
        self.assertTrue(any('stale' in e for e in validate([entry], [row], evidence)['errors']))
        row['target'] = 'Merhaba'
        row['stage'] = 'in-game-verified'
        self.assertTrue(any('in-game' in e for e in validate([entry], [row], evidence)['errors']))
        self.assertTrue(validate([entry], [row])['errors'])

    def test_layout_columns_and_escaped_choice_brackets_are_preserved(self):
        source = r'\[ Damage: \Column 380 {$Value} \]'
        entry = {'key': 'x', 'source': source, 'source_sha256': digest(source.encode()), 'scope': 'game'}
        row = {'key': 'x', 'target': r'\[ Hasar: \Column 380 {$Value} \]',
               'source_sha256': entry['source_sha256'], 'stage': 'draft'}
        self.assertEqual(validate([entry], [row])['errors'], [])
        row['target'] = r'[ Hasar: \Column 38 {$Value} ]'
        self.assertTrue(any('marker' in error for error in validate([entry], [row])['errors']))

    def test_numeric_style_change_detected_even_when_marker_counts_match(self):
        source = 'Damage: {#OldFormat}{$TooltipData.Old}{!Icons.RightArrow}{#UpgradeFormat}{$TooltipData.New}'
        entry = {'key': 'x', 'source': source, 'source_sha256': digest(source.encode()), 'scope': 'game'}
        row = {'key': 'x', 'target': 'Hasar: {#OldFormat}{$TooltipData.Old}{!Icons.RightArrow}{#UpgradeFormat}{$TooltipData.New}',
               'source_sha256': entry['source_sha256'], 'stage': 'draft'}
        self.assertEqual(validate([entry], [row])['warnings'], [])
        row['target'] = 'Hasar: {#UpgradeFormat}{$TooltipData.Old}{!Icons.RightArrow}{#OldFormat}{$TooltipData.New}'
        report = validate([entry], [row])
        self.assertEqual(report['errors'], [])
        self.assertTrue(any('style scope' in warning for warning in report['warnings']))

    def test_styled_tooltip_phrases_can_be_reordered(self):
        source = 'Damage {#OldFormat}{$TooltipData.Damage}; speed {#UpgradeFormat}{$TooltipData.Speed}'
        entry = {'key': 'x', 'source': source, 'source_sha256': digest(source.encode()), 'scope': 'game'}
        row = {'key': 'x', 'target': 'Hız {#UpgradeFormat}{$TooltipData.Speed}; hasar {#OldFormat}{$TooltipData.Damage}',
               'source_sha256': entry['source_sha256'], 'stage': 'draft'}
        self.assertEqual(validate([entry], [row])['warnings'], [])

    def test_ascii_replacement_damage_is_rejected_but_questions_are_allowed(self):
        entry = {'key': 'x', 'source': 'Cast?', 'source_sha256': digest(b'Cast?'), 'scope': 'game'}
        row = {'key': 'x', 'target': 'Büyü? Evet.', 'source_sha256': entry['source_sha256'], 'stage': 'draft'}
        self.assertEqual(validate([entry], [row])['errors'], [])
        row['target'] = 'B?y?'
        self.assertTrue(any('encoding damage' in e for e in validate([entry], [row])['errors']))

    def test_keyword_change_invalidates_review_of_unchanged_sentence(self):
        sentence = {'key': 'sentence', 'source': 'Your {$Keywords.Cast} is stronger.',
                    'source_sha256': 'sentence-hash', 'scope': 'game'}
        term = {'key': 'term', 'file': 'Text/HelpText.en.sjson', 'id': 'Cast::DisplayName::1',
                'source': 'Cast', 'source_sha256': 'term-hash', 'scope': 'game'}
        row = {'key': 'sentence', 'source_sha256': 'sentence-hash',
               'target': '{$Keywords.Cast} güçlenir.', 'stage': 'context-reviewed'}
        term_row = {'key': 'term', 'source_sha256': 'term-hash', 'target': 'Büyü', 'stage': 'draft'}
        review = {'sentence': {'source_sha256': 'sentence-hash', 'target_sha256': digest(row['target'].encode()),
                              'references': ['synthetic weapon'], 'meaning': 'stronger', 'voice': 'UI',
                              'scene': 'weapon tooltip', 'formatting': 'keyword',
                              'keyword_sha256': {'Cast': digest('Büyü'.encode())}}}
        self.assertEqual(validate([sentence, term], [row, term_row], review)['errors'], [])
        term_row['target'] = 'Atış'
        report = validate([sentence, term], [row, term_row], review)
        self.assertTrue(any('keyword review' in error for error in report['errors']))


if __name__ == '__main__':
    unittest.main()
