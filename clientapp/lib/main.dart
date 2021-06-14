import 'package:clientapp/models/auth_model.dart';
import 'package:clientapp/models/current_game_model.dart';
import 'package:clientapp/models/games_list_model.dart';
import 'package:clientapp/pages/login_page.dart';
import 'package:clientapp/services/api.dart';
import 'package:clientapp/services/api_config.dart';
import 'package:clientapp/services/api_mock.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

Future<void> main() async {
  runApp(
    MultiProvider(
      providers: [
        Provider<ApiConfig>(
          create: (_) => ApiConfig(),
        ),
        Provider<Api>(
          create: (context) {
            var config = context.read<ApiConfig>();
            return config.diconnectedMode ? ApiMock() : Api(config);
          },
          dispose: (_, api) => api.disconnect(),
        ),
        ChangeNotifierProvider<AuthData>(
          create: (_) => AuthData(),
        ),
        ChangeNotifierProvider<GamesListModel>(
          create: (_) => GamesListModel(),
        ),
        ChangeNotifierProvider<CurrentGameModel>(
          create: (context) => CurrentGameModel(context.read<Api>().gameUpdates),
        ),
      ],
      child: MyApp(),
    ),
  );
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
