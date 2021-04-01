import 'package:clientapp/models/auth_model.dart';
import 'package:clientapp/models/current_game_model.dart';
import 'package:clientapp/pages/lobbies_page.dart';
import 'package:clientapp/services/api.dart';
import 'package:flutter/material.dart';
import 'package:flutter/widgets.dart';
import 'package:provider/provider.dart';

class GamePage extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    // TODO: switch to model
    var model = context.watch<CurrentGameModel>();
    return Scaffold(
        appBar: AppBar(title: Text("Play game")),
        body: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            _statusWidget(model),
            Table(
              border: TableBorder(
                horizontalInside: BorderSide(width: 3),
                verticalInside: BorderSide(width: 3),
              ),
              children: _buildTableCells(model, context),
            ),
          ],
        ));
  }

  List<TableRow> _buildTableCells(
      CurrentGameModel model, BuildContext context) {
    List<TableRow> result = [];
    for (var i = 0; i < model.gameMap.length; i++) {
      List<Widget> rowItems = [];
      for (var j = 0; j < model.gameMap.length; j++) {
        var gesture = GestureDetector(
          onTap: () => model.generalInfo!.isFilledWithPlayers &&
                  model.participation != UserGameParticipation.readOnly
              ? _tap(i, j, model, context)
              : null,
          child: _toWidget(model.gameMap[i][j]),
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

  Widget _statusWidget(CurrentGameModel model) {
    if (!model.generalInfo!.isFilledWithPlayers) {
      return Text("Waiting for other player to join...");
    }
    switch (model.status) {
      case GameStatus.XTurn:
        switch (model.participation) {
          case UserGameParticipation.readOnly:
            return Text("X turn");
          case UserGameParticipation.playForX:
            return Text("Your turn!");
          case UserGameParticipation.playForO:
            return Text("Waiting for opponent turn X");
        }
      case GameStatus.OTurn:
        switch (model.participation) {
          case UserGameParticipation.readOnly:
            return Text("O turn");
          case UserGameParticipation.playForX:
            return Text("Waiting for opponent turn O");
          case UserGameParticipation.playForO:
            return Text("Your turn!");
        }
      case GameStatus.XWin:
        return model.participation == UserGameParticipation.readOnly
            ? Text("X Won!")
            : model.participation == UserGameParticipation.playForX
                ? Text("You Won!")
                : Text("You lose :( ");
      case GameStatus.OWin:
        return model.participation == UserGameParticipation.readOnly
            ? Text("O Won!")
            : model.participation == UserGameParticipation.playForO
                ? Text("You Won!")
                : Text("You lose :( ");
      case GameStatus.Timeout:
        return Text("Timeout >:[ ");
      default:
        throw UnimplementedError();
    }
  }

  Future<void> _tap(
      int i, int j, CurrentGameModel model, BuildContext context) async {
    switch (model.gameMap[i][j]) {
      case CellStatus.Empty:
        if (model.participation == UserGameParticipation.playForX &&
                model.status == GameStatus.XTurn ||
            model.participation == UserGameParticipation.playForO &&
                model.status == GameStatus.OTurn) {
          model.makeOptimisticTurn(i, j);
          var api = context.read<Api>();
          var userModel = context.read<AuthData>();
          await api.turn(i, j, userModel.authToken!, model.generalInfo!.gameId);
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
