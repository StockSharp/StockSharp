using DevExpress.Xpf.Core;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using SciChart.Examples.ExternalDependencies.Controls.ExceptionView;
using SciChart.Charting.Visuals;
using SciChart.Charting3D;
using SciChart.Charting.Visuals.RenderableSeries;
using SciChart.Drawing.HighSpeedRasterizer;
using System.Windows;
//using DevExpress.Xpf.DemoCenterBase;

namespace SciTrader
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            SciChartSurface.SetRuntimeLicenseKey(@"sg31zM/PSLC8SbWYuZbHgufihdbbfIH4PJQIufrR1ceIob6gdVC5y66clIXrslo2cPjrTUVk/hywZotEPTQjaytYmPqTfudjH2KSJtXx5cQu08BU3RO38G9fsBM+5owhUCFaPvx4Tr3tWaCzhqJbo2HkPcnEc22nUQtt0TZJEE12KBHwMJq6Ja5WY7/+JOVnOuPxAk5oBcOF+aUGK1MqOHlHHQg7+gA9VQO0KrBrtcjubvztbAnyT+7EbAiX7iZVhJyvZEaBWbtS03akejaHQI0WKdsXwI286aAY1gOCYYthaLrC7ZI6qnFn/UaPEq3Fa7y3YkmIqx7JE/9kXThqGlgNDkuMv5RPEArVeYabSIIT9JloBjrc2mYj/H9NoiGIC65lXdYuSBS32cXIMp1Ria9cy+38PtOeWa7vAJiVLr2xcFM75J3zlnzcvj8QV5ul/RB8LhhVMZ0BK5HjEukGXHSA1lrtz3mgqA7ouFhWDLmkwo7F8eUznqeVKPgWr7WZzU2ivRo3Uge5hK3AM9w8AIFCI1Z5o4htozqIpEaoFCnLCCyDe3N1ZemkO5f23VGLcEYhnnvN5DD2FmF7gUKW0h8BwSQxCMrGr/GNFA==");

            ApplicationThemeHelper.ApplicationThemeName = Theme.Office2019ColorfulName;
            //DemoRunner.ShowApplicationSplashScreen();
            base.OnStartup(e);
            DevExpress.Utils.About.UAlgo.Default.DoEventObject(DevExpress.Utils.About.UAlgo.kDemo, DevExpress.Utils.About.UAlgo.pWPF, this);
        }
    }
}
