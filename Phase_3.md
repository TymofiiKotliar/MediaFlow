Phase 3 — Device Management UI
  
  - Replace MediaFlow.API placeholder with a new MediaFlow.Presentation Avalonia project
  - Set up Avalonia app host — call AddMediaFlowServices() in App.axaml.cs, resolve MainWindow from the container
  - Main window navigation shell (sidebar or tab strip to switch between device list and browser)
  - Device list screen — view model bound to GetAllDevicesUseCase and DeleteDeviceUseCase
  - Profile editor screen — form fields for all DeviceProfile properties, OS folder picker dialogs, bound to RegisterDeviceUseCase /
  EditDeviceUseCase
  - Form validation feedback — field-level error labels driven by DeviceProfileValidator
  - Naming template builder control — token-chip UI, cursor-position insertion, live preview label via BuildNamingTemplateUseCase