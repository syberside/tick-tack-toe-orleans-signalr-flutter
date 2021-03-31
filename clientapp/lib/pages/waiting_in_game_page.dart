import 'package:flutter/material.dart';
import 'package:flutter/widgets.dart';

class WaitingInGamePage extends StatelessWidget {
  final String gameId;

  const WaitingInGamePage({Key? key, required this.gameId}) : super(key: key);

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(
          title: Text("Waiting for opponent to join $gameId"),
        ),
        body: Center(
          child: CircularProgressIndicator(),
        ),
      );
}
