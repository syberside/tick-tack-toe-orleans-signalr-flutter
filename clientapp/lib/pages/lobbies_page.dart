import 'package:clientapp/models/auth_model.dart';
import 'package:clientapp/models/games_list_model.dart';
import 'package:clientapp/pages/game_page.dart';
import 'package:clientapp/pages/waiting_in_game_page.dart';
import 'package:clientapp/services/api.dart';
import 'package:flutter/cupertino.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class LobbiesPage extends StatelessWidget {
  final ScrollController _scrollCtrl = ScrollController();

  Future<List<GameGeneralInfo>>? _loadData(BuildContext context) async {
    var api = context.read<Api>();
    var result = await api.getLobbies();
    var model = context.read<GamesListModel>();
    model.replace(result);
    return model.items.toList();
  }

  Future<void> _createNew(BuildContext context) async {
    var playForX = await showDialog<bool?>(
      builder: (BuildContext context) => SimpleDialog(
        title: Text("Please choose your side"),
        children: [
          SimpleDialogOption(
            child: Text("Play for X"),
            onPressed: () => Navigator.pop(context, true),
          ),
          SimpleDialogOption(
            child: Text("Play for O"),
            onPressed: () => Navigator.pop(context, false),
          ),
        ],
      ),
      context: context,
    );
    if (playForX == null) {
      return;
    }
    var api = context.read<Api>();
    var token = context.read<AuthData>().authToken;
    var gameId = await api.createGame(token!, playForX);
    // TODO: Subscribe for game updates
    // TODO: On update => reload in game mode
    Navigator.push(context,
        MaterialPageRoute(builder: (_) => WaitingInGamePage(gameId: gameId)));
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(
          title: Text('Select game or create a new one'),
        ),
        body: Column(
          children: [
            ElevatedButton(
              child: Text("Create new game"),
              onPressed: () => _createNew(context),
            ),
            context.watch<GamesListModel>().isLoaded
                ? _buildListWidget(
                    context.read<GamesListModel>().items.toList())
                : FutureBuilder<List<GameGeneralInfo>>(
                    future: _loadData(context),
                    builder: (context, snapshot) =>
                        snapshot.connectionState == ConnectionState.done &&
                                snapshot.hasData
                            ? _buildListWidget(snapshot.data!)
                            : Center(child: CircularProgressIndicator()),
                  ),
          ],
        ),
      );

  Expanded _buildListWidget(List<GameGeneralInfo> data) => Expanded(
        child: ListView.separated(
            controller: _scrollCtrl,
            padding: const EdgeInsets.all(8),
            itemCount: data.length,
            itemBuilder: (BuildContext context, int index) {
              return Container(
                height: 50,
                child: Center(child: LobbieWidget(data: data[index])),
              );
            },
            separatorBuilder: (_, int index) => const Divider()),
      );
}

class LobbieWidget extends StatelessWidget {
  final GameGeneralInfo data;

  const LobbieWidget({Key? key, required this.data}) : super(key: key);

  @override
  Widget build(BuildContext context) => new Container(
        padding: const EdgeInsets.all(3.0),
        decoration: BoxDecoration(border: Border.all(color: Colors.blueAccent)),
        child: Row(
          mainAxisSize: MainAxisSize.max,
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Expanded(
              child: Padding(
                padding: EdgeInsets.symmetric(horizontal: 2.0),
                child: Text(data.playerX ?? "", textAlign: TextAlign.left),
              ),
            ),
            Text("VS"),
            Expanded(
              child: Padding(
                padding: EdgeInsets.symmetric(horizontal: 2.0),
                child: Text(data.playerO ?? "", textAlign: TextAlign.right),
              ),
            ),
            data.canParticipate
                ? ElevatedButton(
                    onPressed: () => _joinGame(context, data),
                    child: Text('Play'))
                : ElevatedButton(
                    onPressed: () => _viewGame(context, data),
                    child: Text('View')),
          ],
        ),
      );

  void _joinGame(BuildContext context, GameGeneralInfo data) {
    final data = GameData([
      [CellStatus.Empty, CellStatus.Empty, CellStatus.Empty],
      [CellStatus.Empty, CellStatus.Empty, CellStatus.Empty],
      [CellStatus.Empty, CellStatus.Empty, CellStatus.Empty],
    ], GameGeneralInfo("10", "x1", "o2"), GameStatus.XTurn);
    //TODO: Call backend to join game, subscribe to updates
    Navigator.push(
        context,
        MaterialPageRoute(
            builder: (_) => GamePage(
                  data: data,
                  playForX: true,
                )));
  }

  void _viewGame(BuildContext context, GameGeneralInfo data) {
    final data = GameData([
      [CellStatus.X, CellStatus.Empty, CellStatus.O],
      [CellStatus.Empty, CellStatus.X, CellStatus.O],
      [CellStatus.Empty, CellStatus.Empty, CellStatus.X],
    ], GameGeneralInfo("10", "x1", "o2"), GameStatus.XWin);
    //TODO: Call backend to get data, subscribe to updates
    Navigator.push(
        context,
        MaterialPageRoute(
            builder: (_) => GamePage(
                  data: data,
                  playForX: null,
                )));
  }
}

class GameGeneralInfo {
  bool get canParticipate => playerO == null || playerX == null;

  String? playerX;
  String? playerO;
  String gameId;

  GameGeneralInfo(this.gameId, this.playerX, this.playerO);
}

enum GameStatus {
  XTurn,
  OTurn,
  XWin,
  OWin,
  Timeout,
}

enum CellStatus {
  Empty,
  X,
  O,
}

class GameData {
  List<List<CellStatus>> gameMap;
  GameGeneralInfo generalInfo;
  GameStatus status;

  GameData(this.gameMap, this.generalInfo, this.status);
}
