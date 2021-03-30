import 'package:clientapp/pages/game_page.dart';
import 'package:clientapp/pages/waiting_in_game_page.dart';
import 'package:flutter/cupertino.dart';
import 'package:flutter/material.dart';

class LobbiesPage extends StatelessWidget {
  final ScrollController _scrollCtrl = ScrollController();
  final List<GameGeneralInfo> _data = [
    GameGeneralInfo("1", "player X", null, GameStatus.Timeout),
    GameGeneralInfo("2", null, "player O", GameStatus.XTurn),
    GameGeneralInfo("3", "player X", null, GameStatus.Timeout),
    GameGeneralInfo("4", "player x", "player o", GameStatus.OWin),
  ];

  void _createNew(BuildContext context) {
    //TODO: Ask for X or O
    //TODO: Call backend, get id, open game with parameter
    Navigator.push(
        context, MaterialPageRoute(builder: (_) => WaitingInGamePage()));
  }

  //TODO: Get games from backend, ping for games, update games on socket event
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
            Expanded(
              child: ListView.separated(
                  controller: _scrollCtrl,
                  padding: const EdgeInsets.all(8),
                  itemCount: _data.length,
                  itemBuilder: (BuildContext context, int index) {
                    return Container(
                      height: 50,
                      child: Center(child: LobbieWidget(data: _data[index])),
                    );
                  },
                  separatorBuilder: (_, int index) => const Divider()),
            ),
          ],
        ),
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
    ], GameGeneralInfo("10", "x1", "o2", GameStatus.XTurn));
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
    ], GameGeneralInfo("10", "x1", "o2", GameStatus.XWin));
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
  GameStatus status;

  GameGeneralInfo(this.gameId, this.playerX, this.playerO, this.status);
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

  GameData(this.gameMap, this.generalInfo);
}
