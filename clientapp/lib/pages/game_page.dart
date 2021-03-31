import 'package:clientapp/models/current_game_model.dart';
import 'package:clientapp/pages/lobbies_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter/widgets.dart';

class GamePage extends StatefulWidget {
  final GameData data;
  final UserGameParticipation mode;

  GamePage({
    Key? key,
    required this.data,
    required this.mode,
  }) : super(key: key);

  @override
  _GamePageState createState() => _GamePageState();
}

class _GamePageState extends State<GamePage> {
  @override
  Widget build(BuildContext context) => Scaffold(
      appBar: AppBar(title: Text("Play game")),
      body: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          _statusWidget(),
          Table(
            border: TableBorder(
              horizontalInside: BorderSide(width: 3),
              verticalInside: BorderSide(width: 3),
            ),
            children: _buildTableCells(),
          ),
        ],
      ));

  List<TableRow> _buildTableCells() {
    List<TableRow> result = [];
    for (var i = 0; i < widget.data.gameMap.length; i++) {
      List<Widget> rowItems = [];
      for (var j = 0; j < widget.data.gameMap.length; j++) {
        var gesture = GestureDetector(
          onTap: () => widget.data.generalInfo.isFilledWithPlayers &&
                  widget.mode != UserGameParticipation.readOnly
              ? _tap(i, j)
              : null,
          child: _toWidget(widget.data.gameMap[i][j]),
        );
        rowItems.add(gesture);
      }
      var row = TableRow(children: rowItems);
      result.add(row);
    }
    return result;
  }

  Widget _toWidget(CellStatus c) {
    switch (c) {
      case CellStatus.Empty:
        return Icon(
          Icons.cancel_outlined,
          size: 100,
          color: Colors.white,
        );
      case CellStatus.X:
        return Icon(
          Icons.close,
          size: 100,
        );
      case CellStatus.O:
        return Icon(
          Icons.lens_outlined,
          size: 100,
        );
      default:
        throw UnimplementedError();
    }
  }

  Widget _statusWidget() {
    if (!widget.data.generalInfo.isFilledWithPlayers) {
      return Text("Waiting for other player to join...");
    }
    switch (widget.data.status) {
      case GameStatus.XTurn:
        return widget.mode == UserGameParticipation.readOnly
            ? Text("X turn")
            : widget.mode == UserGameParticipation.playForX
                ? Text("Your turn!")
                : Text("Waiting for opponent turn");
      case GameStatus.OTurn:
        return widget.mode == UserGameParticipation.readOnly
            ? Text("O turn")
            : widget.mode == UserGameParticipation.playForY
                ? Text("Your turn!")
                : Text("Waiting for opponent turn");
      case GameStatus.XWin:
        return widget.mode == UserGameParticipation.readOnly
            ? Text("X Won!")
            : widget.mode == UserGameParticipation.playForX
                ? Text("You Won!")
                : Text("You lose :( ");
      case GameStatus.OWin:
        return widget.mode == UserGameParticipation.readOnly
            ? Text("O Won!")
            : widget.mode == UserGameParticipation.playForY
                ? Text("You Won!")
                : Text("You lose :( ");
      case GameStatus.Timeout:
        return Text("Timeout >:[ ");
      default:
        throw UnimplementedError();
    }
  }

  void _tap(int i, int j) {
    switch (widget.data.gameMap[i][j]) {
      case CellStatus.Empty:
        if (widget.mode == UserGameParticipation.playForX &&
                widget.data.status == GameStatus.XTurn ||
            widget.mode == UserGameParticipation.playForY &&
                widget.data.status == GameStatus.OTurn) {
          setState(() {
            widget.data.gameMap[i][j] =
                widget.mode == UserGameParticipation.playForX
                    ? CellStatus.X
                    : CellStatus.O;
            widget.data.status = widget.data.status == GameStatus.XTurn
                ? GameStatus.OTurn
                : GameStatus.XTurn;
          });
          //TODO: call backend
        }
        break;
      case CellStatus.X:
        break;
      case CellStatus.O:
        break;
      default:
        throw UnimplementedError();
    }
  }
}
