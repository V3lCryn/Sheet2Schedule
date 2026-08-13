// System.Windows.Forms (added in newer .NET Windows Forms) and Autodesk.Revit.UI both
// define a TaskDialog class. This project always means the Revit one, so alias it
// globally here rather than qualifying every call site individually.
global using TaskDialog = Autodesk.Revit.UI.TaskDialog;

// Autodesk.Revit.DB also has its own Form class (conceptual mass forms), which collides
// with System.Windows.Forms.Form. This project's UI forms always mean the WinForms one.
global using Form = System.Windows.Forms.Form;

// Same story for Control (Revit has one for MEP/family controls), Color (Revit has one
// for material/graphics colors), and Panel (Revit has one for curtain wall/family
// panels). This project's UI code always means the WinForms/System.Drawing versions.
global using Control = System.Windows.Forms.Control;
global using Color = System.Drawing.Color;
global using Panel = System.Windows.Forms.Panel;
