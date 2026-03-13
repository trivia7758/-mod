extends RefCounted
class_name StoryDSL

const COMMENT_PREFIXES := ["#", "//", ";"]

func parse(text: String) -> Array:
	var events: Array = []
	var lines = text.split("\n")
	var i := 0
	while i < lines.size():
		var raw = lines[i].strip_edges()
		i += 1
		if raw == "" or _is_comment(raw):
			continue
		if raw == "event_choice":
			var choice = {"type": "choice", "options": []}
			while i < lines.size():
				var ln = lines[i].strip_edges()
				i += 1
				if ln == "" or _is_comment(ln):
					continue
				if ln == "end_choice":
					break
				if ln.begins_with("option "):
					var opt = _parse_option(ln)
					if opt:
						choice["options"].append(opt)
			events.append(choice)
			continue
		var tokens = _tokenize(raw)
		if tokens.is_empty():
			continue
		var cmd = String(tokens[0])
		match cmd:
			"event_label":
				if tokens.size() >= 2:
					events.append({"type": "label", "name": String(tokens[1])})
			"event_goto":
				if tokens.size() >= 2:
					events.append({"type": "goto", "label": String(tokens[1])})
			"event_setpos":
				if tokens.size() >= 4:
					events.append({"type": "setpos", "unit": String(tokens[1]), "x": int(tokens[2]), "y": int(tokens[3])})
			"event_move":
				if tokens.size() >= 4:
					events.append({"type": "move", "unit": String(tokens[1]), "x": int(tokens[2]), "y": int(tokens[3])})
			"event_face":
				if tokens.size() >= 3:
					events.append({"type": "face", "unit": String(tokens[1]), "dir": String(tokens[2])})
			"event_talk":
				if tokens.size() >= 3:
					var text_val = _unescape(_join_text(tokens, 2))
					events.append({"type": "talk", "unit": String(tokens[1]), "text": text_val})
			"event_wait":
				if tokens.size() >= 2:
					events.append({"type": "wait", "seconds": float(tokens[1])})
			"event_hide_dialogue":
				events.append({"type": "hide_dialogue"})
			"event_bg":
				if tokens.size() >= 2:
					events.append({"type": "bg", "path": _unescape(_join_text(tokens, 1))})
			"event_location":
				if tokens.size() >= 2:
					events.append({"type": "location", "text": _unescape(_join_text(tokens, 1))})
			"event_morality":
				if tokens.size() >= 2:
					events.append({"type": "morality", "value": int(tokens[1])})
			_:
				push_warning("Unknown DSL command: %s" % cmd)
	return events

func _parse_option(line: String) -> Dictionary:
	var tokens = _tokenize(line)
	if tokens.size() < 4:
		return {}
	# option "text" goto label morality delta
	var text_val = _unescape(_join_text(tokens, 1))
	var goto_label := ""
	var delta := 0
	var idx := 2
	while idx < tokens.size():
		var key = String(tokens[idx])
		if key == "goto" and idx + 1 < tokens.size():
			goto_label = String(tokens[idx + 1])
			idx += 2
		elif key == "morality" and idx + 1 < tokens.size():
			delta = int(tokens[idx + 1])
			idx += 2
		else:
			idx += 1
	return {"text": text_val, "goto": goto_label, "morality_delta": delta}

func _is_comment(line: String) -> bool:
	for p in COMMENT_PREFIXES:
		if line.begins_with(p):
			return true
	return false

func _tokenize(line: String) -> Array[String]:
	var out: Array[String] = []
	var buf := ""
	var in_quote := false
	var i := 0
	while i < line.length():
		var ch = line[i]
		if ch == "\"":
			in_quote = not in_quote
		elif ch == " " and not in_quote:
			if buf != "":
				out.append(buf)
				buf = ""
		else:
			buf += ch
		i += 1
	if buf != "":
		out.append(buf)
	return out

func _join_text(tokens: Array, start: int) -> String:
	var parts: Array[String] = []
	for i in range(start, tokens.size()):
		parts.append(String(tokens[i]))
	var text = " ".join(parts)
	if text.begins_with("\"") and text.ends_with("\"") and text.length() >= 2:
		text = text.substr(1, text.length() - 2)
	return text

func _unescape(text: String) -> String:
	return text.replace("\\n", "\n").replace("\\\"", "\"")
