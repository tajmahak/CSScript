## Установка

1. Распаковать утилиту в постоянную папку (например: "C:\CSScript\");
2. Запустить "register.bat" для регистрации утилиты в реестре.

Для проверки работоспособности можно запустить "HelloWorld.cssc".
Перед удалением утилиты запустить "unregister.bat" для очистки информации из реестра.

## Описание

```
Утилита для выполнения C# скриптов.
────────────────────────────────────────────────────────────────────────────────────────────────────
ПРАВИЛА СОСТАВЛЕНИЯ СКРИПТОВ:

- Скрипты сохраняются в *.cssc файлы. Скрипты могут быть исполняемыми, могут быть библиотеками,
  функции которых используются другими скриптами.

- При запуске код скрипта преобразуется в компилируемый код C# и запускается в собственном классе-
  контейнере с методом Start() для запуска кода.

- Код скрипта состоит из процедурного кода и других допонительных блоков:

  #using              Конструкция C# using. Блок может использоваться в любой части кода. Пример:
                      │ #using System.Text
                      │ #using System.Threading

  #import             Подключение других скриптов или сборок Windows, используемых в скрипте
                      (*.cssc, *.exe, *.dll). Блок может использоваться в любой части кода. Пример:
                      │ #import script.cssc
                      │ #import someLibrary.dll
                      │ #import System.Design

  #class              Код внутри класса контейнера скомпилированного скрипта. Количество строк кода
                      ограничивается объявлением другого блока (кроме однострочных блоков #using и
                                          #import). Пример:
                      │ #class
                      │ // Код внутри класса-контейнера

  #ns, #namespace     Код внутри пространства имён, в котором находится класс контейнера
                                          скомпилированного скрипта. Количество строк кода ограничивается объявлением
                                          другого блока (кроме однострочных блоков #using и #import). Пример:
                      │ #namespace
                      │ // Код внутри пространства имён

  #init                           Код инициализации, выполняемый в методе Start() до начала процедурного кода
                      запускаемого скрипта. Может использоваться *.cssc библиотеками, т.к.
                                          процедурный код подключаемого скрипта игнорируется. Пример:
                      │ #init
                      │ // Код инициализации
────────────────────────────────────────────────────────────────────────────────────────────────────
ЗАПУСК:

Синтаксис команды: CSScript.exe [SCRIPT_PATH] [OPTIONS]

Дополнительные опции:

  -h                 Выполнение скрипта в скрытом режиме.

  -p                 После окончания работы скрипта принудительно ожидать от пользователя
                     подтверждение закрытия окна (не работает в скрытом режиме).

  -a <SCRIPT_ARGS>   Параметры командной строки, передаваемые утилитой скрипту.

  -reg               Регистрация утилиты в реестре Windows для поддержки .cssc файлов. При указании
                     опции путь к скрипту не требуется.

  -unreg             Удаление регистрации утилиты из реестра Windows. При указании опции путь к
                     скрипту не требуется.
────────────────────────────────────────────────────────────────────────────────────────────────────
ВЗАИМОДЕЙСТВИЕ С КОНТЕКСТОМ:

Для взаимодействия скрипта с утилитой в классе-контейнере используется свойство 'Context',
объявленное в классе контейнера:

│ Context: IScriptContext
│   - реализация интерфейса для взаимодействия скрипта с контекстом:
│
│  .Args: string[]
│    - получает входящие аргументы командной строки;
│
│  .ExitCode: int
│    - получает или задаёт код возврата скрипта;
│
│  .Pause: bool
│    - получает или задаёт значение, следует ли ожидать от пользователя подтверждение закрытия;
│
│  .ScriptPath: string
│    - получает абсолютный путь к исполняемому скрипту;
│
│  .OutLog: IList<LogFragment>
│    - получает лог работы скрипта (стандартный выходной поток);
│
│  .ErrorLog: IList<LogFragment>
│    - получает лог работы скрипта (стандартный поток ошибок);
│
│  .Write(value: object, [color: ConsoleColor]): void
│    - вывод сообщения в лог;
│
│  .WriteLine([value: object], [color: ConsoleColor]): void
│    - вывод сообщения в лог с переносом строки;
│
│  .WriteError(value: object): void
│    - вывод сообщения в лог ошибок;
│
│  .WriteErrorLine([value: object]): void
│    - вывод сообщения в лог ошибок с переносом строки;
│
│  .ReadLine([caption: string], [color: ConsoleColor]): string
│    - чтение текста из входного потока;
│
│  .RegisterProcess(process: Process): void
│    - регистрация созданного процесса для автозакрытия в случае ошибки в работе скрипта;

Также в классе-контейнере есть свойство 'Colors', представляющее цветовую схему:

│ Colors: ColorScheme
│   - получает цвета для использования в консоли (аналог Context.ColorScheme);
│
│  .Foreground
│    - цвет основного текста;
│
│  .Caption
│    - цвет заголовка;
│
│  .Info
│    - цвет информационного текста;
│
│  .Success
│    - цвет сообщения об успешной операции;
│
│  .Error
│    - цвет сообщения об ошибке;
────────────────────────────────────────────────────────────────────────────────────────────────────
ПРИМЕР СКРИПТА:

│ // Выполнение кода в основной области (внутри процедуры)
│ PrintHelloWorld();
│
│ // Начало области внутри класса
│ #class
│
│ void PrintHelloWorld() {
│     string scriptName = Path.GetFileName(Context.ScriptPath);
│     Context.WriteLine("Hello World from " + scriptName, Colors.Caption);
│ }
│
│ // Директиву using допустимо использовать в любой части скрипта
│ #using System.IO;
```
