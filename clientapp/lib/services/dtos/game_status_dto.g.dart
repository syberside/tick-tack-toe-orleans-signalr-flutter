// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'game_status_dto.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

GameStatusDto _$GameStatusDtoFromJson(Map<String, dynamic> json) {
  return GameStatusDto(
    status: _$enumDecode(_$GameStatusEnumMap, json['status']),
    gameMap: GameMapDto.fromJson(json['gameMap'] as Map<String, dynamic>),
    playerXName: json['playerXName'] as String?,
    playerOName: json['playerOName'] as String?,
  );
}

Map<String, dynamic> _$GameStatusDtoToJson(GameStatusDto instance) =>
    <String, dynamic>{
      'status': _$GameStatusEnumMap[instance.status],
      'gameMap': instance.gameMap,
      'playerXName': instance.playerXName,
      'playerOName': instance.playerOName,
    };

K _$enumDecode<K, V>(
  Map<K, V> enumValues,
  Object? source, {
  K? unknownValue,
}) {
  if (source == null) {
    throw ArgumentError(
      'A value must be provided. Supported values: '
      '${enumValues.values.join(', ')}',
    );
  }

  return enumValues.entries.singleWhere(
    (e) => e.value == source,
    orElse: () {
      if (unknownValue == null) {
        throw ArgumentError(
          '`$source` is not one of the supported values: '
          '${enumValues.values.join(', ')}',
        );
      }
      return MapEntry(unknownValue, enumValues.values.first);
    },
  ).key;
}

const _$GameStatusEnumMap = {
  GameStatus.XTurn: 0,
  GameStatus.OTurn: 1,
  GameStatus.XWin: 2,
  GameStatus.OWin: 3,
  GameStatus.Timeout: 4,
  GameStatus.Draw: 5,
};
