using static __Utils;

namespace CSScriptStand
{
    internal class Stand : CSScript.Core.ScriptContainer
    {
        public Stand(CSScript.Core.IScriptContext context) : base(context) {
            // Инициализация (в скрипте он внедняется в класс вместо неработающей конструкции using static)
            __Utils_Init(Context);
        }

        public override void Start() {


            FileList a = GetFiles(@"D:\Admin\Desktop\Новая папка")
                  .SortFilesByWriteTime(true)
                  .Delete(3);
        }



    }
}
