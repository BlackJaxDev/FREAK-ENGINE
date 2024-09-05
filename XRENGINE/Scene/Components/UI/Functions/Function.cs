﻿//using Extensions;
//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.ComponentModel;
//using System.Drawing;
//using System.Linq;
//using System.Reflection;
//using System.Windows.Forms;
//using XREngine.Components;
//using XREngine.Components.Scene.Mesh;
//using XREngine.Core.Maths;
//using XREngine.Core.Maths.Transforms;
//using XREngine.Rendering.Models.Materials;
//using XREngine.Rendering.Text;

//namespace XREngine.Rendering.UI.Functions
//{
//    public class FunctionDefinition : Attribute
//    {
//        public FunctionDefinition(
//            string category,
//            string name,
//            string description,
//            string keywords,
//            int operationCount = 1)
//        {
//            Keywords = keywords.Split(' ').ToList();
//            Description = description;
//            Name = name;
//            Category = category;
//            OperationCount = operationCount;
//        }

//        public List<string> Keywords { get; }
//        public string Name { get; }
//        public string Description { get; }
//        public string Category { get; }
//        public int OperationCount { get; }
//    }
//    public interface IFunction : IUIComponent
//    {

//    }
//    public abstract class BaseFunction : UIMaterialComponent, IShaderVarOwner, IFunction
//    {
//        public static Vector4 RegularColor { get; set; } = new Vector4(0.1f, 0.1f, 0.1f, 1.0f);
//        public static Vector4 SelectedColor { get; set; } = new Vector4(0.1f, 0.2f, 0.25f, 1.0f);
//        public static Vector4 HighlightedColor { get; set; } = new Vector4(0.1f, 0.3f, 0.4f, 1.0f);
        
//        [Browsable(false)]
//        public FunctionDefinition Definition => GetType().GetCustomAttribute<FunctionDefinition>();
//        [Browsable(false)]
//        public ReadOnlyCollection<string> Keywords => Definition?.Keywords.AsReadOnly();
//        [Browsable(false)]
//        public string FunctionName => Definition?.Name;
//        [Browsable(false)]
//        public string Description => Definition?.Description;
//        [Browsable(false)]
//        public string Category => Definition?.Category;

//        protected UITextRasterComponent _headerText;
//        protected UIString2D _headerString;
//        protected List<UITextRasterComponent> _inputParamTexts = new List<UITextRasterComponent>();
//        protected List<UITextRasterComponent> _outputParamTexts = new List<UITextRasterComponent>();

//        protected const int HeaderPadding = 2;
//        protected Font _paramFont = new Font("Segoe UI", 9.0f, FontStyle.Regular);
//        protected Font _headerFont = new Font("Segoe UI", 11.0f, FontStyle.Bold);

//        public BaseFunction() : base(MakeFunctionMaterial())
//        {
//            _headerText = new UITextRasterComponent
//            {
//                Name = FunctionName + " [Header Text]",
//                VerticalAlignment = EVerticalAlign.Top,
//                Height = TextRenderer.MeasureText(FunctionName, _headerFont).Height + HeaderPadding * 2,
//            };
//            _headerString = new UIString2D()
//            {
//                Text = FunctionName,
//                Font = _headerFont,
//                Format = TextFormatFlags.NoClipping | TextFormatFlags.SingleLine
//            };
//            _headerText.TextDrawer.Text.Add(_headerString);
//            ChildSockets.Add(_headerText);
//        }

//        private static XRMaterial MakeFunctionMaterial()
//        {
//            return XRMaterial.CreateUnlitColorMaterialForward(new ColorF4(0.1f, 1.0f));
//        }

//        public abstract void GetMinMax(out Vector2 min, out Vector2 max, bool searchForward = true, bool searchBackward = true);

//        protected void AddParam(BaseFuncArg arg)
//        {
//            ChildSockets.Add(arg);

//            UITextRasterComponent text = new UITextRasterComponent { Name = arg.Name + " Text", };
//            text.TextDrawer.Text.Add(new UIString2D(arg.Name, _paramFont, Color.White, TextFormatFlags.NoClipping | TextFormatFlags.SingleLine));
//            ChildSockets.Add(text);

//            if (arg is IFuncExecInput || arg is IFuncValueInput)
//            {
//                _inputParamTexts.Add(text);
//            }
//            else if (arg is IFuncExecOutput || arg is IFuncValueOutput)
//            {
//                _outputParamTexts.Add(text);
//            }
//        }

//        public override string ToString()
//            => FunctionName;

//        public static List<Type> Find<T>(string keywords) where T : IFunction
//        {
//            string[] keyArray = keywords.Split(' ');
//            Dictionary<int, List<Type>> types = new Dictionary<int, List<Type>>();
//            IEnumerable<Type> functions = Assembly.GetExecutingAssembly().GetTypes().
//                Where(x => x is T && x.IsDefined(typeof(FunctionDefinition)));
//            foreach (Type func in functions)
//            {
//                if (func.IsAbstract)
//                    continue;

//                FunctionDefinition def = func.GetCustomAttribute<FunctionDefinition>();

//                int count = 0;
//                foreach (string keyword in def.Keywords)
//                {
//                    foreach (string typedKeyword in keyArray)
//                        if (keyword.Contains(typedKeyword))
//                        {
//                            ++count;
//                            break;
//                        }
//                }
//                if (count > 0)
//                {
//                    if (types.ContainsKey(count))
//                        types[count].Add(func);
//                    else
//                        types.Add(count, new List<Type>() { func });
//                }
//            }
//            int maxVal = TMath.Max(types.Keys.ToArray());
//            return types[maxVal];
//        }
//    }
//    /// <summary>
//    /// Provides a hud component that can connect to other functions with parameters and execution flow.
//    /// Use FunctionDefinition attribute to specify information to display on the UI. 
//    /// Nothing else is necessary for custom functions to show to users.
//    /// </summary>
//    /// <typeparam name="TVIn">The input value argument class to use.</typeparam>
//    /// <typeparam name="TVOut">The output value class to use.</typeparam>
//    /// <typeparam name="TEIn">The input execution argument class to use.</typeparam>
//    /// <typeparam name="TEOut">The output execution class to use.</typeparam>
//    public abstract class Function<TVIn, TVOut, TEIn, TEOut> : BaseFunction
//        where TVIn : BaseFuncValue, IFuncValueInput
//        where TVOut : BaseFuncValue, IFuncValueOutput
//        where TEIn : BaseFuncExec, IFuncExecInput
//        where TEOut : BaseFuncExec, IFuncExecOutput
//    {
//        public Function(bool deferControlArrangement = false) : base()
//        {
//            if (!deferControlArrangement)
//                CollectArguments();
//        }

//        protected virtual void CollectArguments()
//        {
//            AddExecInput(GetExecInputs());
//            AddExecOutput(GetExecOutputs());
//            AddValueInput(GetValueInputs());
//            AddValueOutput(GetValueOutputs());
//            ArrangeControls();
//        }

//        public override void GetMinMax(out Vector2 min, out Vector2 max, bool searchForward = true, bool searchBackward = true)
//        {
//            RemakeAxisAlignedRegion();

//            BaseFunction func;
//            min = RenderInfo2D.AxisAlignedRegion.Min;
//            max = RenderInfo2D.AxisAlignedRegion.Max;

//            if (searchBackward)
//            {
//                foreach (var input in _execInputs)
//                {
//                    func = input?.ConnectedToGeneric?.OwningFunction;
//                    if (func is null)
//                        continue;
//                    func.GetMinMax(out Vector2 min2, out Vector2 max2, false, true);
//                    min = Vector2.ComponentMin(min, min2);
//                    max = Vector2.ComponentMax(max, max2);
//                }
//                foreach (var input in _valueInputs)
//                {
//                    func = input?.Connection?.OwningFunction;
//                    if (func is null)
//                        continue;
//                    func.GetMinMax(out Vector2 min2, out Vector2 max2, false, true);
//                    min = Vector2.ComponentMin(min, min2);
//                    max = Vector2.ComponentMax(max, max2);
//                }
//            }
//            if (searchForward)
//            {
//                foreach (var output in _execOutputs)
//                {
//                    func = output?.ConnectedToGeneric?.OwningFunction;
//                    if (func is null)
//                        continue;
//                    func.GetMinMax(out Vector2 min2, out Vector2 max2, true, false);
//                    min = Vector2.ComponentMin(min, min2);
//                    max = Vector2.ComponentMax(max, max2);
//                }
//                foreach (var output in _valueOutputs)
//                {
//                    var list = output?.Connections;
//                    foreach (var input in list)
//                    {
//                        func = input?.OwningFunction;
//                        if (func is null)
//                            continue;
//                        func.GetMinMax(out Vector2 min2, out Vector2 max2, true, false);
//                        min = Vector2.ComponentMin(min, min2);
//                        max = Vector2.ComponentMax(max, max2);
//                    }
//                }
//            }
//        }

//        #region Input/Output Exec
//        protected List<TEIn> _execInputs = new List<TEIn>();
//        protected List<TEOut> _execOutputs = new List<TEOut>();
//        [Browsable(false)]
//        public List<TEIn> InputExec => _execInputs;
//        [Browsable(false)]
//        public List<TEOut> OutputExec => _execOutputs;
//        protected virtual TEIn[] GetExecInputs() => null;
//        protected virtual TEOut[] GetExecOutputs() => null;
//        protected void AddExecInput(TEIn[] input) => input.ForEach(AddExecInput);
//        protected void AddExecOutput(TEOut[] output) => output.ForEach(AddExecOutput);
//        protected void AddExecInput(TEIn input)
//        {
//            _execInputs.Add(input);
//            input.OwningFunction = this;
//            AddParam(input);
//        }
//        protected void AddExecOutput(TEOut output)
//        {
//            _execOutputs.Add(output);
//            output.OwningFunction = this;
//            AddParam(output);
//        }
//        #endregion

//        #region Input/Output Values
//        protected List<TVIn> _valueInputs = new List<TVIn>();
//        protected List<TVOut> _valueOutputs = new List<TVOut>();
//        [Browsable(false)]
//        public List<TVIn> InputArguments => _valueInputs;
//        [Browsable(false)]
//        public List<TVOut> OutputArguments => _valueOutputs;
//        protected virtual TVIn[] GetValueInputs() => null;
//        protected virtual TVOut[] GetValueOutputs() => null;
//        protected void AddValueInput(TVIn[] input) => input.ForEach(AddValueInput);
//        protected void AddValueOutput(TVOut[] output) => output.ForEach(AddValueOutput);
//        protected void AddValueInput(TVIn input)
//        {
//            input.ArgumentIndex = _valueInputs.Count;
//            _valueInputs.Add(input);
//            input.OwningFunction = this;
//            AddParam(input);
//        }
//        protected void AddValueOutput(TVOut output)
//        {
//            output.ArgumentIndex = _valueOutputs.Count;
//            _valueOutputs.Add(output);
//            output.OwningFunction = this;
//            AddParam(output);
//        }
//        #endregion

//        #region Control Arrangement
//        public void ArrangeControls()
//        {
//            //Vector2 headerSize = _headerString.Region.Extents;
//            //int totalHeaderPadding = HeaderPadding * 2;
//            //headerSize.Y += totalHeaderPadding;
//            //headerSize.X += totalHeaderPadding;
            
//            //int connectionBoxBounds = BaseFuncArg.ConnectionBoxDims + BaseFuncArg.ConnectionBoxMargin;
//            //int rows = Math.Max(
//            //    _valueInputs.Count + _execInputs.Count,
//            //    _valueOutputs.Count + _execOutputs.Count);

//            //Size[] inputTextSizes = new Size[rows];
//            //Size[] outputTextSizes = new Size[rows];
//            //int[] maxHeights = new int[rows];
//            //int maxRows = Math.Max(inputTextSizes.Length, outputTextSizes.Length);

//            //int middleMargin = 2;

//            //int maxRowWidth = 0;
//            //int maxRowHeight = 0;
//            //int currentRowWidth;
//            //_size.Y = headerSize.Y + BaseFuncArg.ConnectionBoxMargin * 2.0f;
//            //for (int i = 0; i < maxRows; ++i)
//            //{
//            //    currentRowWidth = middleMargin;

//            //    if (i < _execInputs.Count)
//            //        Arrange1(_execInputs[i], i, inputTextSizes, ref currentRowWidth);
//            //    else if (i - _execInputs.Count < _valueInputs.Count)
//            //        Arrange1(_valueInputs[i - _execInputs.Count], i, inputTextSizes, ref currentRowWidth);

//            //    if (i < _execOutputs.Count)
//            //        Arrange1(_execOutputs[i], i, outputTextSizes, ref currentRowWidth);
//            //    else if (i - _execOutputs.Count < _valueOutputs.Count)
//            //        Arrange1(_valueOutputs[i - _execOutputs.Count], i, outputTextSizes, ref currentRowWidth);

//            //    maxRowWidth = Math.Max(maxRowWidth, currentRowWidth);
//            //    maxRowHeight = TMath.Max(connectionBoxBounds,
//            //        i < inputTextSizes.Length ? inputTextSizes[i].Height : 0,
//            //        i < outputTextSizes.Length ? outputTextSizes[i].Height : 0);
//            //    maxHeights[i] = maxRowHeight;
//            //    _size.Y += maxRowHeight;
//            //}

//            //_size.X = Math.Max(maxRowWidth, headerSize.X);

//            //SizeableWidth.SetSizingPixels(_size.X);
//            //SizeableHeight.SetSizingPixels(_size.Y);

//            //float yTrans = _size.Y - headerSize.Y - BaseFuncArg.ConnectionBoxMargin;
//            //for (int i = 0; i < maxRows; ++i)
//            //{
//            //    int height = TMath.Max(connectionBoxBounds,
//            //        i < inputTextSizes.Length ? inputTextSizes[i].Height : 0,
//            //        i < outputTextSizes.Length ? outputTextSizes[i].Height : 0);

//            //    yTrans -= height;

//            //    if (i < _execInputs.Count)
//            //        Arrange2(_execInputs[i], _inputParamTexts[i], inputTextSizes[i], true, headerSize.Y, yTrans, maxHeights[i]);
//            //    else if (i - _execInputs.Count < _valueInputs.Count)
//            //        Arrange2(_valueInputs[i - _execInputs.Count], _inputParamTexts[i], inputTextSizes[i], true, headerSize.Y, yTrans, maxHeights[i]);

//            //    if (i < _execOutputs.Count)
//            //        Arrange2(_execOutputs[i], _outputParamTexts[i], outputTextSizes[i], false, headerSize.Y, yTrans, maxHeights[i]);
//            //    else if (i - _execOutputs.Count < _valueOutputs.Count)
//            //        Arrange2(_valueOutputs[i - _execOutputs.Count], _outputParamTexts[i], outputTextSizes[i], false, headerSize.Y, yTrans, maxHeights[i]);
//            //}

//            ////_headerText.LocalTranslation = new Vector2(0.0f, _size.Y);

//            //Resize();
//        }
//        private void Arrange2(BaseFuncArg arg, UITextRasterComponent text, Size size, bool input, float headerHeight, float yTrans, int maxRowHeight)
//        {
//            //text.Size = size;
//            //int t = BaseFuncArg.ConnectionBoxDims + BaseFuncArg.ConnectionBoxMargin;

//            //float xTrans;
//            //if (input)
//            //{
//            //    xTrans = BaseFuncArg.ConnectionBoxMargin;
//            //    arg.LocalOriginPercentage = new Vector2(0.0f, 0.6f);

//            //    text.LocalOriginPercentage = new Vector2(0.0f, 0.0f);
//            //}
//            //else
//            //{
//            //    xTrans = _size.X - BaseFuncArg.ConnectionBoxMargin;
//            //    arg.LocalOriginPercentage = new Vector2(1.0f, 0.6f);

//            //    t = -t;
//            //    text.LocalOriginPercentage = new Vector2(1.0f, 0.0f);
//            //}
//            //arg.LocalTranslation = new Vector2(xTrans, yTrans);
//            //text.LocalTranslation = new Vector2(xTrans + t, yTrans);
//            //arg.LocalTranslationY += maxRowHeight * 0.5f;
//        }
//        private void Arrange1(BaseFuncArg arg, int i, Size[] sizes, ref int currentRowWidth)
//        {
//            Size argTextSize = TextRenderer.MeasureText(arg.Name, _paramFont);
//            sizes[i] = argTextSize;
//            currentRowWidth += BaseFuncArg.ConnectionBoxDims + BaseFuncArg.ConnectionBoxMargin * 2 + argTextSize.Width;
//        }
//        #endregion

//        protected override void OnChildAdded(ISocket item)
//        {
//            base.OnChildAdded(item);
//            if (OwningActor != null)
//            {
//                foreach (var e in _execInputs)
//                {
//                    OwningActor.RootComponent.ChildSockets.Add(e);
//                }
//                foreach (var v in _valueInputs)
//                {
//                    OwningActor.RootComponent.ChildSockets.Add(v);
//                }
//            }
//        }
//    }
//}
