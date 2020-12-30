Утилита позволяет выполнять запуск C# кода, написанного в файле с раширением ```.cssc```.

### Установка утилиты

1. Поместите бинарные файлы утилиты в любую директорию (например: ```C:\CSScript\```);
1. Для регистрации программы и расширения ```.cssc``` в оболочке Windows выполните ```CSScript.exe -reg``` (для удаления регистрации: ```CSScript.exe -unreg```);

### Аргументы командной строки

#### Утилита

- ```{...}``` - путь к файлу скрипта;
- ```[-h[ide]]``` - выполнение скрипта в скрытом режиме;
- ```[-p[ause]]``` - после окончания работы скрипта ожидать от пользователя подтверждение закрытия;
- ```[-a[rgs] {...} [{...} ...]]``` - входящие аргументы для использования в скрипте (```Context.Args: string[]```);
- ```[-reg]``` - регистрация программы и расширения ```.cssc``` в оболочке Windows;
- ```[-unreg]``` - удаление регистрации программы и расширения ```.cssc``` в оболочке Windows;

#### CSSC-скрипт

- ```[-h[ide]]``` - выполнение скрипта в скрытом режиме;
- ```[-p[ause]]``` - после окончания работы скрипта ожидать от пользователя подтверждение закрытия;
- ```{...} [{...} ...]]``` - входящие аргументы для использования в скрипте (```Context.Args: string[]```);

### Служебный синтаксис скрипта

- ```#using {...}``` - конструкция C# using. Используется # в начале строки;
- ```#import {...}``` - подключение другого скрипта или сборки Windows (*.exe, *.dll);
- ```#class``` - обозначение начала области внутри класса;
- ```#ns``` / ```#namespace``` - обозначение начала области внутри пространства имён;

### Взаимодействие с контекстом

- ```Context: IScriptContext``` - реализация интерфейса для взаимодействия скрипта с контекстом:
    - ```.Args: string[]``` - получает входящие аргументы командной строки;
    - ```.ExitCode: int``` - получает или задаёт код возврата скрипта;
    - ```.Pause: bool``` - получает или задаёт значение, следует ли ожидать от пользователя подтверждение закрытия;
    - ```.ScriptPath: string``` - получает абсолютный путь к исполняемому скрипту;
    - ```.Write(value: object, [color: ConsoleColor]): void``` - вывод сообщения в лог;
    - ```.WriteLine([value: object], [color: ConsoleColor]): void``` - вывод сообщения в лог с переносом строки;
    - ```.ReadLine([caption: string], [color: ConsoleColor]): string``` - чтение текста из входного потока;
    - ```.CreateManagedProcess(): Process``` - создание процесса, контролируемого окружением (автозакрытие в случае ошибки);
- ```Colors: ColorScheme``` - получает цвета для использования в консоли (аналог: ```Context.ColorScheme```);
    - ```.Background``` - цвет фона;
    - ```.Foreground``` - цвет основного текста;
    - ```.Caption``` - цвет заголовка;
    - ```.Info``` - цвет информационного текста;
    - ```.Success``` - цвет сообщения об успешной операции;
    - ```.Error``` - цвет сообщения об ошибке;

### Пример скрипта

```C#
// Выполнение кода в основной области (внутри процедуры)
PrintHelloWorld();
Context.Pause = true;

// Начало области внутри класса
#class

void PrintHelloWorld() {
 string scriptName = Path.GetFileName(Context.ScriptPath);
 Context.WriteLine("Hello World from " + scriptName, Colors.Caption);
}

// Директиву using допустимо использовать в любой части скрипта
#using System.IO;
```
