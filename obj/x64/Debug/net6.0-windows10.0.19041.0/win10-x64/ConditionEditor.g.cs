﻿#pragma checksum "G:\GrandMaisonHRG\Tools\src\GMTool\ConditionEditor.xaml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "BBEAE7330C6C0EAF78AF2AEDF1F9137679BB4D0CE5FEF112AACDFFCE4EF51E0B"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace GMTool
{
    partial class ConditionEditor : 
        global::Microsoft.UI.Xaml.Controls.Page, 
        global::Microsoft.UI.Xaml.Markup.IComponentConnector
    {

        /// <summary>
        /// Connect()
        /// </summary>
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.UI.Xaml.Markup.Compiler"," 3.0.0.2312")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void Connect(int connectionId, object target)
        {
            switch(connectionId)
            {
            case 2: // ConditionEditor.xaml line 49
                {
                    this.tbParam1 = global::WinRT.CastExtensions.As<global::Microsoft.UI.Xaml.Controls.NumberBox>(target);
                }
                break;
            case 3: // ConditionEditor.xaml line 50
                {
                    this.tbParam2 = global::WinRT.CastExtensions.As<global::Microsoft.UI.Xaml.Controls.NumberBox>(target);
                }
                break;
            case 4: // ConditionEditor.xaml line 51
                {
                    this.tbParam3 = global::WinRT.CastExtensions.As<global::Microsoft.UI.Xaml.Controls.NumberBox>(target);
                }
                break;
            case 5: // ConditionEditor.xaml line 52
                {
                    this.tbDynamicParams = global::WinRT.CastExtensions.As<global::Microsoft.UI.Xaml.Controls.TextBox>(target);
                }
                break;
            case 6: // ConditionEditor.xaml line 22
                {
                    this.cbConditionId = global::WinRT.CastExtensions.As<global::Microsoft.UI.Xaml.Controls.ComboBox>(target);
                    ((global::Microsoft.UI.Xaml.Controls.ComboBox)this.cbConditionId).SelectionChanged += this.CbConditionId_OnSelectionChanged;
                }
                break;
            case 7: // ConditionEditor.xaml line 29
                {
                    this.cbCompareType = global::WinRT.CastExtensions.As<global::Microsoft.UI.Xaml.Controls.ComboBox>(target);
                }
                break;
            case 8: // ConditionEditor.xaml line 36
                {
                    this.tbTargetValue = global::WinRT.CastExtensions.As<global::Microsoft.UI.Xaml.Controls.NumberBox>(target);
                }
                break;
            default:
                break;
            }
            this._contentLoaded = true;
        }

        /// <summary>
        /// GetBindingConnector(int connectionId, object target)
        /// </summary>
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.UI.Xaml.Markup.Compiler"," 3.0.0.2312")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::Microsoft.UI.Xaml.Markup.IComponentConnector GetBindingConnector(int connectionId, object target)
        {
            global::Microsoft.UI.Xaml.Markup.IComponentConnector returnValue = null;
            return returnValue;
        }
    }
}

