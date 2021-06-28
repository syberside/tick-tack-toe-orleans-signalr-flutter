import 'package:json_annotation/json_annotation.dart';
import 'package:clientapp/data/cell_status.dart';

part 'game_map_dto.g.dart';

@JsonSerializable()
class GameMapDto {
  GameMapDto({
    required this.data,
  });
  factory GameMapDto.fromJson(Map<String, dynamic> json) => _$GameMapDtoFromJson(json);
  Map<String, dynamic> toJson() => _$GameMapDtoToJson(this);

  List<List<CellStatus>> data;
}
