// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'game_map_dto.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

GameMapDto _$GameMapDtoFromJson(Map<String, dynamic> json) {
  return GameMapDto(
    data: (json['data'] as List<dynamic>)
        .map((e) => (e as List<dynamic>)
            .map((e) => _$enumDecode(_$CellStatusEnumMap, e))
            .toList())
        .toList(),
  );
}

Map<String, dynamic> _$GameMapDtoToJson(GameMapDto instance) =>
    <String, dynamic>{
      'data': instance.data
          .map((e) => e.map((e) => _$CellStatusEnumMap[e]).toList())
          .toList(),
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

const _$CellStatusEnumMap = {
  CellStatus.Empty: 0,
  CellStatus.X: 1,
  CellStatus.O: 2,
};
