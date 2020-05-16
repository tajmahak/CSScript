using SharpShell.Attributes;
using SharpShell.SharpDropHandler;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace CSScript.ShellExtension
{
    [ComVisible(true)]
    [Guid("DA86D195-0C8D-477D-9038-98E53B5E29B0")]
    [COMServerAssociation(AssociationType.ClassOfExtension, ".cssc")]
    public class ShellDropHandler : SharpDropHandler
    {
        protected override void DragEnter(DragEventArgs dragEventArgs)
        {
            dragEventArgs.Effect = DragDropEffects.Link;
        }

        protected override void Drop(DragEventArgs dragEventArgs)
        {
            List<string> args = new List<string>();
            args.Add(SelectedItemPath);
            args.Add("/a");
            args.AddRange(DragItems);

            StringBuilder a = new StringBuilder();
            for (int i = 0; i < args.Count; i++)
            {
                a.AppendLine(i + ") " + args[i]);
            }
            MessageBox.Show(
                
                a.ToString()
                
                );

            Program.Main(args.ToArray());
            //Process.Start(SelectedItemPath, args.ToString());
        }
    }
}
