using System.Collections.Generic;
using UnityEngine;

namespace CaoCao.Story
{
    public class StoryDSLParser
    {
        static readonly string[] CommentPrefixes = { "#", "//", ";" };

        public List<StoryEvent> Parse(string text)
        {
            var events = new List<StoryEvent>();
            var lines = text.Split('\n');
            int i = 0;

            while (i < lines.Length)
            {
                string raw = lines[i].Trim();
                i++;

                if (string.IsNullOrEmpty(raw) || IsComment(raw))
                    continue;

                if (raw == "event_choice")
                {
                    var choice = new ChoiceEvent();
                    while (i < lines.Length)
                    {
                        string ln = lines[i].Trim();
                        i++;
                        if (string.IsNullOrEmpty(ln) || IsComment(ln))
                            continue;
                        if (ln == "end_choice")
                            break;
                        if (ln.StartsWith("option "))
                        {
                            var opt = ParseOption(ln);
                            if (opt != null)
                                choice.Options.Add(opt);
                        }
                    }
                    events.Add(choice);
                    continue;
                }

                var tokens = Tokenize(raw);
                if (tokens.Count == 0)
                    continue;

                string cmd = tokens[0];
                switch (cmd)
                {
                    case "event_label":
                        if (tokens.Count >= 2)
                            events.Add(new LabelEvent(tokens[1]));
                        break;
                    case "event_goto":
                        if (tokens.Count >= 2)
                            events.Add(new GotoEvent(tokens[1]));
                        break;
                    case "event_setpos":
                        if (tokens.Count >= 4)
                            events.Add(new SetPosEvent(tokens[1], int.Parse(tokens[2]), int.Parse(tokens[3])));
                        break;
                    case "event_move":
                        if (tokens.Count >= 4)
                            events.Add(new MoveEvent(tokens[1], int.Parse(tokens[2]), int.Parse(tokens[3])));
                        break;
                    case "event_face":
                        if (tokens.Count >= 3)
                            events.Add(new FaceEvent(tokens[1], tokens[2]));
                        break;
                    case "event_talk":
                        if (tokens.Count >= 3)
                            events.Add(new TalkEvent(tokens[1], Unescape(JoinText(tokens, 2))));
                        break;
                    case "event_wait":
                        if (tokens.Count >= 2)
                            events.Add(new WaitEvent(float.Parse(tokens[1])));
                        break;
                    case "event_hide_dialogue":
                        events.Add(new HideDialogueEvent());
                        break;
                    case "event_bg":
                        if (tokens.Count >= 2)
                            events.Add(new BackgroundEvent(Unescape(JoinText(tokens, 1))));
                        break;
                    case "event_location":
                        if (tokens.Count >= 2)
                            events.Add(new LocationEvent(Unescape(JoinText(tokens, 1))));
                        break;
                    case "event_morality":
                        if (tokens.Count >= 2)
                            events.Add(new MoralityEvent(int.Parse(tokens[1])));
                        break;
                    default:
                        Debug.LogWarning($"[StoryDSL] Unknown command: {cmd}");
                        break;
                }
            }
            return events;
        }

        ChoiceOption ParseOption(string line)
        {
            var tokens = Tokenize(line);
            if (tokens.Count < 4)
                return null;

            // option "text" goto label morality delta
            string text = Unescape(JoinText(tokens, 1));
            string gotoLabel = "";
            int delta = 0;
            int idx = 2;

            while (idx < tokens.Count)
            {
                string key = tokens[idx];
                if (key == "goto" && idx + 1 < tokens.Count)
                {
                    gotoLabel = tokens[idx + 1];
                    idx += 2;
                }
                else if (key == "morality" && idx + 1 < tokens.Count)
                {
                    int.TryParse(tokens[idx + 1], out delta);
                    idx += 2;
                }
                else
                {
                    idx++;
                }
            }

            return new ChoiceOption { Text = text, GotoLabel = gotoLabel, MoralityDelta = delta };
        }

        static bool IsComment(string line)
        {
            foreach (var p in CommentPrefixes)
                if (line.StartsWith(p))
                    return true;
            return false;
        }

        static List<string> Tokenize(string line)
        {
            var result = new List<string>();
            string buf = "";
            bool inQuote = false;

            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];
                if (ch == '"')
                {
                    inQuote = !inQuote;
                }
                else if (ch == ' ' && !inQuote)
                {
                    if (buf.Length > 0)
                    {
                        result.Add(buf);
                        buf = "";
                    }
                }
                else
                {
                    buf += ch;
                }
            }
            if (buf.Length > 0)
                result.Add(buf);

            return result;
        }

        static string JoinText(List<string> tokens, int start)
        {
            var parts = new List<string>();
            for (int i = start; i < tokens.Count; i++)
                parts.Add(tokens[i]);
            string text = string.Join(" ", parts);
            if (text.StartsWith("\"") && text.EndsWith("\"") && text.Length >= 2)
                text = text.Substring(1, text.Length - 2);
            return text;
        }

        static string Unescape(string text)
        {
            return text.Replace("\\n", "\n").Replace("\\\"", "\"");
        }
    }
}
