import 'package:flutter/material.dart';
import 'package:flutter/widgets.dart';

class WaitingInGamePage extends StatelessWidget {
  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(
          title: Text("Waiting for opponent to join"),
        ),
        body: Center(
          child: CircularProgressIndicator(),
        ),
      );
}
