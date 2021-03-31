import 'package:clientapp/pages/lobbies_page.dart';
import 'package:flutter/foundation.dart';

class GamesListModel extends ChangeNotifier {
  bool _isLoaded = false;
  final List<GameGeneralInfo> _items = [];

  Iterable<GameGeneralInfo> get items => _items;

  bool get isLoaded => _isLoaded;

  void replace(Iterable<GameGeneralInfo> result) {
    _isLoaded = true;
    _items.clear();
    _items.addAll(result);
  }
}
