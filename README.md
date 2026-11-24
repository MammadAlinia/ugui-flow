# uGUI Flow

uGUI Flow is a Unity UI package for responsive layouts, prefab-based themes, screen navigation, and reactive data binding.

## Install with Unity Package Manager

In Unity 6.0 or later with Git on your `PATH`, open **Window > Package Management > Package Manager > + > Install package from git URL**, paste the URL below, and click **Install**.

```text
https://github.com/MammadAlinia/ugui-flow.git?path=/Packages/com.mammadalinia.ugui-flow
```

## Development

To run the demo scenes and tests below, open this repository as a project in Unity **6000.0.85f1**. The embedded package is `Packages/com.mammadalinia.ugui-flow`.

For the setup milestone, confirm that **uGUI Flow** appears under **In Project** in Package Manager, then run `PackageSmokeTests` from **Window > General > Test Runner > EditMode**. With the project closed, you can also run the tests from the repository root with the Unity CLI.

```powershell
unity test . --mode EditMode --output Logs/editmode-results.xml
```

The package code is licensed under MIT. See [LICENSE.md](LICENSE.md).

## Responsive layout demo

Open `Assets/Demos/ResponsiveLayout/ResponsiveLayout.unity` and enter Play Mode. Try a 1040 × 780 Game view for two columns and 440 × 804 for one column. At the narrower size, scroll the advanced settings to reach **Restore defaults**.

Drag **Column**, **Row**, or **Grid** from `Packages/com.mammadalinia.ugui-flow/Runtime/Layout/Prefabs` into a Canvas. Each prefab starts with full-stretch anchors. When you nest one under a Row or Column that controls child size, use its `LayoutElement` to set minimum, preferred, or flexible size. Grid assigns its cell size to each direct child.

Place visual prefabs directly under a layout group in display order. Use **Container** when you need a single layout child whose own children are positioned by their RectTransforms. The Container fills a plain parent by default. When you put it inside a Row or Column that controls child size, add a `LayoutElement` to give the Container itself a size.

Add **Responsive Grid** alongside **Grid Layout Group** when the layout should reflow by width. Stretch the grid horizontally or have its parent layout group assign its width. Set the breakpoint and wide column count on **Responsive Grid**, and set the cell height, padding, and spacing on **Grid Layout Group**. For a scrolling layout, use a Column as the `ScrollRect` content and fit its height with a `ContentSizeFitter`.

Run `ResponsiveLayoutTests` in the Test Runner's **PlayMode** tab to verify that the grid reflows its direct children in sibling order.

## Themes

Open `Assets/Demos/Themes/ThemePreview.unity` in Play Mode. Change Music or Volume, then switch among **Light**, **Dark**, **Custom button**, and **Alternate font**. The custom option restyles Reset settings. The alternate option changes the font across the demo. The downloads section shows a themed badge, divider, and read-only progress bar. Set the progress prefab's `Slider.value` from code to update it.

The package includes Light and Dark theme databases under `Runtime/Theming/Defaults`. In a Unity project that has yet to use TMP, import **Window > TextMeshPro > Import TMP Essential Resources** for the default Liberation Sans font and shaders. Add a `ThemeScope` to a Canvas or layout root and assign a database under **Initial Theme**. Put the themed control prefabs directly under layout groups.

To customize a theme, duplicate its database. Change **Default Font** for a font-only theme, or assign a prefab variant to its named Inspector field, such as **Reset Button Prefab**. The variant inherits its `ThemeViewKey` from the base prefab. Each field accepts a prefab whose key uses that field's **Kind**. Assign the fields your themed views use. If you created a database with the earlier **Prefabs** list, assign its references to the named fields and set **Kind** on any custom keys. Place a decorative themed panel alongside its layout content so each `ThemeView` owns only its visual and control hierarchy.

Call `ThemeScope.TrySetTheme(theme, out error)` in Play Mode. The scope preserves supported uGUI control values, label content, and external event listeners. Subscribe to `ViewReplaced` if a controller caches component references. Run `ThemeScopeTests` in Play Mode to check the sample's switch behavior.

The theme preview reserves its heading and description heights with `LayoutElement`. Set heights this way when controls should stay in place across font changes, and leave enough room for the tallest font and wrapped text at the narrowest supported width.

The Downloads section also shows a Card, an Icon Button, and Primary, Secondary, and Tertiary buttons. Edit a Card's title and body through its `ThemeView` content labels. Set an Icon Button's sprite on its content image. The theme scope carries those values across a prefab swap.

## Scrollable shop demo

Open `Assets/Demos/Shop/ShopPreview.unity` in Play Mode to browse 30 supply cards. Use the mouse wheel, drag the list, or move the scrollbar. The grid uses three columns at the wide size and one at the narrow size. Switch between **Light** and **Dark** while scrolled to see the cards change theme in place.

Edit the catalog in the **Shop Preview** component on `Assets/Demos/Shop/ShopPreviewScreen.prefab`. The demo creates each Card prefab directly under the `ScrollRect` content Grid in catalog order.

## Screen lifecycle demo

Open `Assets/Demos/ScreenLifecycle/ScreenLifecycle.unity` in Play Mode. Enable **Keep notes** on **Field notes**, add cards, and switch to **Review notes**. Change the theme and return to **Field notes** to see the same toggle value, cards, and scroll position. **Hide** and **Show** preserve the selected tab. **Reset** and **Close** followed by **Open** start a fresh screen. The lifecycle log shows when the pages initialize, show, hide, and are destroyed.

Click **Test error** on either tab to replace that page with **View unavailable**. You can still switch tabs and change the theme. Click **Reset** to restore both pages.

Click **Test task error** beside the sample task to replace just that row. **Add card** and **Remove card** still work on the notes below it.

Click **Info dialog** or **Error dialog** above the notes to open an overlay in the current theme. Choose **OK**, **Retry**, or **Cancel** to record the action in the lifecycle log.

Add **Window** to a Canvas, assign a **Screen Host** RectTransform on or below the Canvas, and optionally assign a **Startup Screen** prefab. Put **UI Component** or a subclass on the prefab root. Add more components where layouts, pages, or controls need their own lifecycle. The Inspector displays their parents and children based on the hierarchy, including through ordinary layout wrappers.

Each demo mounts its UI from a screen prefab. Edit the prefab to change the layout and use the scene Canvas to configure the Window and input.

Override `OnInitialize`, `OnShow`, `OnHide`, and `OnDispose`, or subscribe to the corresponding C# events `Initialized`, `Shown`, `Hidden`, and `Destroyed`. Initialization runs before the screen activates. Initialize and show run parent first. Hide and dispose run child first. Keep mounted component GameObjects active and enabled, and call `SetVisible(bool)` to change local visibility. Hidden components retain state and active scripts, and the CanvasGroup disables their input. Keep stateful components above replaceable `ThemeView` visuals. Use `ThemeScope.TryApplyCurrentTheme` when adding a new visual subtree.

Every mounted screen has its own `ScreenEvents`. Call `Events.Publish(message)` or `Events.Subscribe<T>(this, handler)` from components in that tree. Dispose each subscription when its listening period ends.

Call `ShowError()` when a component cannot display its content. Lifecycle and screen-event callbacks that fail use the same recovery path. The component and its children are deactivated, and the theme's **Error View Prefab** takes their place. Every theme database requires this slot. Screens without a `ThemeScope` use the bundled Light error view.

From code, call `window.TryOpen(prefab, out var screen, out var error)`, `window.TryHide()`, `window.TryShow()`, and `window.TryClose()`. Use `window.CurrentScreen` to access the mounted root while it is visible or hidden.

## Modal dialogs

Call `Dialog.Info` or `Dialog.Error` from the Unity main thread in Play Mode. Each preset supplies its own header icon. Pass a title, a description, and one or more button actions. An empty or null description leaves out the description area.

```csharp
using UguiFlow;
using UnityEngine;

Dialog.Info("Settings saved", "Your changes are ready.",
    (Icons.Accept, () => Debug.Log("Accepted")),
    (Icons.Cancel, () => Debug.Log("Cancelled")));

Dialog.Error("Download failed", "Check your connection and try again.",
    ("Retry", RetryDownload),
    ("Cancel", CancelDownload));

Dialog.Error("Connection lost", "", () => Debug.Log("Acknowledged"));
```

Icon tuples display a sprite. Label tuples display the text you supply. A single callback displays **OK**. The overlay blocks background UI input while keeping the current screen visible. Use Up and Down to move between actions or scroll the panel when its content exceeds the available height. The dialog closes before the selected callback runs, and queued dialogs open in request order. Unloading the host's scene discards any outstanding dialogs.

The first call creates a dialog overlay in the active scene when needed. To customize it, add one active **Dialog Host** to a dedicated root Canvas in that scene. Create prefab variants from **Info** and **Error** under `Runtime/Dialogs/Resources/UguiFlow/Dialogs`, then assign your variants to **Info Prefab** and **Error Prefab**. Edit the header icon, spacing, and action sprite and button prefab fields on the variants. Add a **Theme Scope** to the same Canvas and assign an **Initial Theme** to apply its panel, label, and button styles while the dialog is open.

## Tabs and page navigation

1. Add **Page Navigator** to the page container inside a screen prefab. Add each page as a **UI Component** child of the navigator and assign it in **Pages**. The first entry is the initial page.
2. Drag **Navigation Bar** from `Packages/com.mammadalinia.ugui-flow/Runtime/Navigation/Prefabs` into the same screen. Assign its **Navigator**, then assign each tab's **Page**. Edit the text on the tab's child button. Add more **Tab** instances under **Tabs** to add destinations.
3. For a custom layout, add **Navigation Bar** and place **Navigation Tab** components beneath it. Assign a child **Button** and a destination **Page** on each tab. You can assign a non-raycastable underline to the bar's **Indicator**. Its horizontal anchors follow the selected tab.

Use `navigator.TrySelectPage(page)` with a page instance from the mounted screen and read `navigator.SelectedPage` for the current destination. A selection keeps the other pages mounted so they retain their state. Subscribe to `PageChanged` through `ScreenEvents` to receive the navigator and the previous and current pages.
