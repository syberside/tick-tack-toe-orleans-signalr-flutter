import 'package:clientapp/app.dart';
import 'package:clientapp/services/api.dart';
import 'package:clientapp/services/api_config.dart';
import 'package:flutter/material.dart';
import 'package:logger/logger.dart';
import 'package:provider/provider.dart';

Future<void> main() async {
  runApp(
    buildApp(
      (ctx) => Api(ctx.read<ApiConfig>(), ctx.read<Logger>()),
    ),
  );
}
