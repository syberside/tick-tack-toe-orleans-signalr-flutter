import 'package:flutter/material.dart';
import 'package:http/io_client.dart';
import 'package:signalr_core/signalr_core.dart';
import 'dart:io';
import 'dart:io' show Platform;
import 'package:flutter/foundation.dart' show kIsWeb;

Future<void> main() async {
  final host = kIsWeb
      ? 'localhost'
      : Platform.isAndroid
          ? '10.0.2.2'
          : 'localhost';
  final connection = HubConnectionBuilder()
      .withUrl(
          'http://$host:5000/chatHub',
          HttpConnectionOptions(
            client: IOClient(
                HttpClient()..badCertificateCallback = (x, y, z) => true),
            logging: (level, message) => print(message),
          ))
      .build();

  await connection.start();

  connection.on('ReceiveMessage', (message) {
    print(message.toString());
  });

  await connection.invoke('SendMessage', args: ['Bob', 'Says hi!']);

  runApp(MyApp());
}

class MyApp extends StatelessWidget {
  // This widget is the root of your application.
  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Flutter Demo',
      theme: ThemeData(
        // This is the theme of your application.
        //
        // Try running your application with "flutter run". You'll see the
        // application has a blue toolbar. Then, without quitting the app, try
        // changing the primarySwatch below to Colors.green and then invoke
        // "hot reload" (press "r" in the console where you ran "flutter run",
        // or simply save your changes to "hot reload" in a Flutter IDE).
        // Notice that the counter didn't reset back to zero; the application
        // is not restarted.
        primarySwatch: Colors.blue,
      ),
      home: MyHomePage(title: 'Flutter Demo Home Page'),
    );
  }
}

class MyHomePage extends StatefulWidget {
  MyHomePage({Key? key, this.title}) : super(key: key);

  final String? title;

  @override
  _MyHomePageState createState() => _MyHomePageState();
}

class _MyHomePageState extends State<MyHomePage> {
  int _counter = 0;

  void _incrementCounter() {
    setState(() {
      _counter++;
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(widget.title!),
      ),
      body: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: <Widget>[
            Text(
              'You have pushed the button this many times:',
            ),
            Text(
              '$_counter',
              style: Theme.of(context).textTheme.headline4,
            ),
          ],
        ),
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: _incrementCounter,
        tooltip: 'Increment',
        child: Icon(Icons.add),
      ), // This trailing comma makes auto-formatting nicer for build methods.
    );
  }
}
