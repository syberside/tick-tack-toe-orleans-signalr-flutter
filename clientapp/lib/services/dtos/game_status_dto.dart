import 'package:clientapp/services/dtos/game_map_dto.dart';
import 'package:json_annotation/json_annotation.dart';
import 'package:clientapp/data/cell_status.dart';
import 'package:clientapp/data/game_status.dart';

part 'game_status_dto.g.dart';

@JsonSerializable()
class GameStatusDto {
  GameStatusDto({
    required this.status,
    required this.gameMap,
    this.playerXName,
    this.playerOName,
  });
  factory GameStatusDto.fromJson(Map<String, dynamic> json) => _$GameStatusDtoFromJson(json);
  Map<String, dynamic> toJson() => _$GameStatusDtoToJson(this);

  GameStatus status;

  GameMapDto gameMap;

  String? playerXName;

  String? playerOName;
}
