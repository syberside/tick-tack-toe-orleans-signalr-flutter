import 'package:clientapp/models/auth_model.dart';
import 'package:clientapp/models/current_game_model.dart';
import 'package:clientapp/models/games_list_model.dart';
import 'package:clientapp/pages/login_page.dart';
import 'package:clientapp/services/api.dart';
import 'package:clientapp/services/api_config.dart';
import 'package:flutter/material.dart';
import 'package:flutter/widgets.dart';
import 'package:logger/logger.dart';
import 'package:provider/provider.dart';

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

Widget buildApp(Api Function(BuildContext) apiProviderBuilder) {
  return MultiProvider(
    providers: [
      Provider(create: (_) => Logger()),
      Provider(create: (_) => ApiConfig()),
      Provider<Api>(
        create: apiProviderBuilder,
        dispose: (ctx, api) => api.disconnect(),
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
  );
}
