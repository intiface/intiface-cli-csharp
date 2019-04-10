# 0.4.3.2 (2019-04-09)

- Implement --stayopen, allowing multiple connections per process lifetime
- Remove --websocketserver, now implied by --wsinsecureport and --wssecureport.
- --guipipe is now --frontendpipe.
- Websocket options now use slightly shorter "ws" prefix.
- Can now listen on IPC and websocket at the same time.
- Fires clientconnection/clientclosed to frontend.

# 0.4.3.1 (2019-04-04)

- Make executable fire ProcessStarted to parent when servers come up.

# 0.4.3.0 (2019-03-31)

- Update to buttplug-c# 0.4.3
  - Fixes issues with bluetooth device reconnection.

# 0.4.2.1 (2019-03-30)

- Add install location output to installer.

# 0.4.2.0 (2019-03-29)

- Split CLI from buttplug-csharp repo.
  - Makes it so we aren't version lockstepped against the library.
  - Want to keep all executables/apps Intiface branded.
  - I like making life difficult on anyone who wants to contribute.
- Add Installer.
- Sign Everything.
- major/minor/patch version tracks buttplug-csharp library we're
  building against. Build number tracks our changes to the CLI itself
  within that version.
