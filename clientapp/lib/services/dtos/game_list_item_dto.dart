import 'package:json_annotation/json_annotation.dart';

part 'game_list_item_dto.g.dart';

@JsonSerializable()
class GameListItemDto {
  GameListItemDto({
    required this.gameId,
    this.playerXName,
    this.playerOName,
  });
  factory GameListItemDto.fromJson(Map<String, dynamic> json) => _$GameListItemDtoFromJson(json);
  Map<String, dynamic> toJson() => _$GameListItemDtoToJson(this);

  String gameId;

  String? playerXName;

  String? playerOName;
}
