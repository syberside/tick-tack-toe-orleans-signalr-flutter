import 'package:clientapp/models/auth_model.dart';
import 'package:clientapp/models/current_game_model.dart';
import 'package:clientapp/models/games_list_model.dart';
import 'package:clientapp/pages/login_page.dart';
import 'package:clientapp/services/api.dart';
import 'package:clientapp/services/api_config.dart';
import 'package:clientapp/services/api_mock.dart';
import 'package:flutter/material.dart';
import 'package:logger/logger.dart';
import 'package:provider/provider.dart';

Future<void> main() async {
  runApp(
    MultiProvider(
      providers: [
        Provider(create: (_) => Logger()),
        Provider(create: (_) => ApiConfig()),
        Provider<Api>(
          create: (ctx) {
            var config = ctx.read<ApiConfig>();
            return config.diconnectedMode ? ApiMock() : Api(config, ctx.read<Logger>());
          },
          dispose: (_, api) => api.disconnect(),
        ),
        ChangeNotifierProvider(create: (_) => AuthData()),
        ChangeNotifierProvider(create: (_) => GamesListModel()),
        ChangeNotifierProvider(
            create: (ctx) => CurrentGameModel(
                  ctx.read<Api>().gameUpdates,
                  ctx.read<Logger>(),
                )),
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
