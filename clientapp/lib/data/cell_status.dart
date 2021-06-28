import 'package:json_annotation/json_annotation.dart';

enum CellStatus {
  @JsonValue(0)
  Empty,

  @JsonValue(1)
  X,

  @JsonValue(2)
  O,
}
