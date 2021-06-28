import 'package:json_annotation/json_annotation.dart';

enum GameStatus {
  @JsonValue(0)
  XTurn,

  @JsonValue(1)
  OTurn,

  @JsonValue(2)
  XWin,

  @JsonValue(3)
  OWin,

  @JsonValue(4)
  Timeout,
}
