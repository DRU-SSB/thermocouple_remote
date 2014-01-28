'******Установка MSComm*************

'Инициализация "системных" объектов
Set sho = CreateObject("WSCRIPT.SHELL")
Set fso = CreateObject("Scripting.FileSystemObject")
targetpath=sho.ExpandEnvironmentStrings("%windir%\system32\MSCOMM32.OCX")

scriptname=WScript.ScriptFullName
sourcedir=""

i=len(scriptname)
do until mid(scriptname,i,1)="\"
    i=i-1
loop
sourcepath=left(scriptname,i) & "MSCOMM32.OCX"
regfilepath=left(scriptname,i) & "VBCTRLS.REG"

fso.CopyFile sourcepath, targetpath

Call sho.Run("%windir%\SYSTEM32\REGSVR32.EXE %windir%\SYSTEM32\MSCOMM32.OCX", 1, True)
Call sho.Run("regedit VBCTRLS.REG", 1, True)


'******Установка SEADDP*************
'Инициализация "системных" объектов
targetpath=sho.ExpandEnvironmentStrings("%windir%\system32\SEADDP.dll")

scriptname=WScript.ScriptFullName
sourcedir=""

i=len(scriptname)
do until mid(scriptname,i,1)="\"
    i=i-1
loop
sourcepath=left(scriptname,i) & "SEADDP.dll"

'Разрегистрация предыдущей версии seaddp.dll, если есть
Call sho.Run("%windir%\SYSTEM32\REGSVR32.EXE %windir%\SYSTEM32\SEADDP.dll -u", 1, True)

fso.CopyFile sourcepath, targetpath

'Регистрация новой версии seaddp.dll
Call sho.Run("%windir%\SYSTEM32\REGSVR32.EXE %windir%\SYSTEM32\SEADDP.dll", 1, True)