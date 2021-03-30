import 'package:flutter/material.dart';

import 'pages/login_page.dart';
//import 'package:http/io_client.dart';
//import 'package:signalr_core/signalr_core.dart';
//import 'dart:io';
//import 'dart:io' show Platform;
//import 'package:flutter/foundation.dart' show kIsWeb;

Future<void> main() async {
  // final host = kIsWeb
  //     ? 'localhost'
  //     : Platform.isAndroid
  //         ? '10.0.2.2'
  //         : 'localhost';
  // final connection = HubConnectionBuilder()
  //     .withUrl(
  //         'http://$host:5000/chatHub',
  //         HttpConnectionOptions(
  //           client: IOClient(
  //               HttpClient()..badCertificateCallback = (x, y, z) => true),
  //           logging: (level, message) => print(message),
  //         ))
  //     .build();

  // await connection.start();

  // connection.on('GameUpdated', (message) {
  //   print("RECEIVED");
  //   print(message.toString());
  // });

  // await connection
  //     .invoke('Watch', args: ['364b499d-4d63-497b-b41a-d21469ad65a8']);

  runApp(MyApp());
}

class MyApp extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Flutter Demo',
      theme: ThemeData(
        primarySwatch: Colors.blue,
      ),
      home: LoginPage(),
    );
  }
}
