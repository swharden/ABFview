rmdir /s /q ..\src\ABFview\bin\
dotnet publish ../src
cd ..\src\ABFview\bin\Debug\net6.0-windows
rename publish ABFview
tar -a -cf ABFview-1.X.zip ABFview
move ABFview-1.X.zip ..\..\..\..\..\website\download
cd ..\..\..\..\..\website\download
explorer .
pause