// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'game_list_item_dto.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

GameListItemDto _$GameListItemDtoFromJson(Map<String, dynamic> json) {
  return GameListItemDto(
    gameId: json['gameId'] as String,
    playerXName: json['playerXName'] as String?,
    playerOName: json['playerOName'] as String?,
  );
}

Map<String, dynamic> _$GameListItemDtoToJson(GameListItemDto instance) =>
    <String, dynamic>{
      'gameId': instance.gameId,
      'playerXName': instance.playerXName,
      'playerOName': instance.playerOName,
    };
