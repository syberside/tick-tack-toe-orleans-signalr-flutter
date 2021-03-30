import 'package:clientapp/pages/lobbies_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter/widgets.dart';

class GamePage extends StatefulWidget {
  final GameData data;
  final bool? playForX;

  GamePage({
    Key? key,
    required this.data,
    required this.playForX,
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
          onTap: () => widget.playForX == null ? null : _tap(i, j),
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
    switch (widget.data.generalInfo.status) {
      case GameStatus.XTurn:
        return widget.playForX == null
            ? Text("X turn")
            : widget.playForX == true
                ? Text("Your turn!")
                : Text("Waiting for opponent turn");
      case GameStatus.OTurn:
        return widget.playForX == null
            ? Text("O turn")
            : widget.playForX == false
                ? Text("Your turn!")
                : Text("Waiting for opponent turn");
      case GameStatus.XWin:
        return widget.playForX == null
            ? Text("X Won!")
            : widget.playForX == true
                ? Text("You Won!")
                : Text("You lose :( ");
      case GameStatus.OWin:
        return widget.playForX == null
            ? Text("O Won!")
            : widget.playForX == false
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
        if (widget.playForX! &&
                widget.data.generalInfo.status == GameStatus.XTurn ||
            !widget.playForX! &&
                widget.data.generalInfo.status == GameStatus.OTurn) {
          setState(() {
            widget.data.gameMap[i][j] =
                widget.playForX! ? CellStatus.X : CellStatus.O;
            widget.data.generalInfo.status =
                widget.data.generalInfo.status == GameStatus.XTurn
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
