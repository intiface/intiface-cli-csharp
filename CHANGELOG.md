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
