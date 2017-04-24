TypeScriptGroupCompilerTool
===========================

This is a simple frontend for the TypeScript compiler (tsc). It provides a way
to compile TypeScript files in separate groups (i.e. one group for each page
on your site).

This tool is intended for classic ASP.NET projects, where Visual Studio
normally compiles all TypeScript files in one group. If you're using npm in
your project, you should probably install a tool like gulp to handle this sort
of task.

Instructions
------------

* Place TypeScriptGroupCompilerTool.exe in a directory (ideally, the root directory of your project.) You can rename it if you like.
* Create an .ini file with the same name as the .exe file (i.e. TypeScriptGroupCompilerTool.ini). Place it in the same folder.
* Set up your groups in the .ini file, using paths relative to the directory you put the .exe and .ini in.
* To run, double-click the .exe in Windows Explorer. The console window will close automatically if (and only if) no errors are encountered.

If a tsconfig.json is present in the folder that you place the .exe and .ini in, its settings will be used.

Configuration
-------------

The .ini file can contain one or more compilation groups. Each group contains
a list of files to compile together:

    [group1]
	scripts/shared1.ts
	scripts/shared2.ts
	scripts/file1.ts

	[group2]
	scripts/shared1.ts
	scripts/shared2.ts
	scripts/file2.ts
	scripts/file3.ts

You can also include groups in other groups. Any group that is included in
another won't be compiled on its own.

    [shared]
	scripts/shared1.ts
	scripts/shared2.ts

	[group1]
	shared
	scripts/file1.ts

	[group2]
	shared
	scripts/file2.ts
	scripts/file3.ts

If you want to specify the path to the TypeScript compiler, you can do so at
the top of the .ini file. IF you don't specify a path, the program will look
for tsc.exe in the current directory, and then in Program Files (using the
newest compiler version available.)

	TypeScriptCompilerPath=C:\Program Files (x86)\Microsoft SDKs\TypeScript\2.1\tsc.exe

	[group1]
	...

Downloads
---------

https://github.com/IsaacSchemm/TypeScriptGroupCompilerTool/releases
