import 'package:clientapp/app.dart';
import 'package:clientapp/services/api_mock.dart';
import 'package:flutter/material.dart';

Future<void> main() async {
  runApp(
    buildApp(
      (ctx) => ApiMock(),
    ),
  );
}
