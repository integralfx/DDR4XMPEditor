using System;
using Stylet;
using StyletIoC;
using DDR4XMPEditor.Pages;

namespace DDR4XMPEditor
{
    public class Bootstrapper : Bootstrapper<ShellViewModel>
    {
        protected override void ConfigureIoC(IStyletIoCBuilder builder)
        {
            // Configure the IoC container in here
        }

        protected override void Configure()
        {
            // Perform any other configuration before the application starts
        }
    }
}
