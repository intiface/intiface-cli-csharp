# 0.5.9.0 (2020-06-17)

- Updated Buttplug to 0.5.9
- Create package generation for Buttplug Unity distribution

# 0.5.5.0 (2019-12-06)

- Updated Buttplug to 0.5.5, adding more Magic Motion toy support.

# 0.5.4.0 (2019-11-26)

- Updated Buttplug to 0.5.4, adding LiBo/Lelo/WeVibe/Lovehoney toy
  support.

# 0.5.3.0 (2019-10-03)

- Updated Buttplug to 0.5.3, fixing issue with JSON packing in
  websocket server.

# 0.5.1.0 (2019-09-28)

- Updated Buttplug to 0.5.1, adding Motorbunny support.

# 0.5.0.0 (2019-07-28)

- Update Buttplug to 0.5.0, adding Realov and Kiiroo Clinoa toy
  support, changing device config to take json instead of YAML.

# 0.4.7.0 (2019-06-30)

- Update Buttplug to 0.4.7, adding LiBo/Svakom toy support, fixing
  Vorze support.

# 0.4.6.0 (2019-05-27)

- Update Buttplug to 0.4.6, fixing BouncyCastle dep collision when
  using secure websockets.

# 0.4.5.0 (2019-05-26)

- Update Buttplug to 0.4.5, including Kiiroo Onyx 2.1 and RealTouch
  support.

# 0.4.3.2 (2019-04-09)

- Implement --stayopen, allowing multiple connections per process
  lifetime
- Remove --websocketserver, now implied by --wsinsecureport and
  --wssecureport.
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
