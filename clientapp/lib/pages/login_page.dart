import 'package:clientapp/models/auth_model.dart';
import 'package:clientapp/pages/lobbies_page.dart';
import 'package:clientapp/services/api.dart';
import 'package:flutter/material.dart';
import 'package:flutter/widgets.dart';
import 'package:provider/provider.dart';

class LoginPage extends StatefulWidget {
  @override
  _LoginPageState createState() => _LoginPageState();
}

/// TODO: authorization on data exists
class _LoginPageState extends State<LoginPage> {
  final _formKey = GlobalKey<FormState>();
  final TextEditingController _inputCtrl = new TextEditingController();
  bool isLoading = false;

  Future<void> _login(BuildContext context) async {
    if (!_formKey.currentState!.validate()) {
      return;
    }
    setState(() => isLoading = true);
    var api = context.read<Api>();
    await api.connect();
    var authCode = await api.login(_inputCtrl.text);
    context.read<AuthData>().storeToken(authCode);
    // TODO: handle errors and timeouts
    Navigator.pushReplacement(
      context,
      MaterialPageRoute(builder: (_) => LobbiesPage()),
    );
    //TODO: Call back to login, get token, store username and token in device storage
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(
          title: Text("Welcome to Tic Tac Toe!"),
        ),
        body: Center(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: <Widget>[
              Form(
                key: _formKey,
                child: Column(
                  children: <Widget>[
                    Padding(
                      padding: const EdgeInsets.all(8.0),
                      child: TextFormField(
                        validator: (value) {
                          if (value == null || value.isEmpty) {
                            return 'Please enter user name';
                          }
                          return null;
                        },
                        controller: _inputCtrl,
                        decoration: InputDecoration(
                            border: OutlineInputBorder(),
                            hintText: 'How can we call you?'),
                      ),
                    ),
                  ],
                ),
              ),
              ElevatedButton(
                child: Text("Login"),
                onPressed: isLoading ? null : () async => await _login(context),
              )
            ],
          ),
        ),
      );
}
