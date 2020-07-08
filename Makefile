UNITY_EDITOR_DIR ?= C:/Program Files/Unity/Hub/Editor/2019.3.15f1
UNITYENGINE_DLLS := -r:"$(UNITY_EDITOR_DIR)/Editor/Data/Managed/UnityEngine/UnityEngine.CoreModule.dll"
UNITYENGINE_DLLS += -r:"$(UNITY_EDITOR_DIR)/Editor/Data/Managed/UnityEngine/UnityEngine.VFXModule.dll"
UNITYENGINE_DLLS += -r:"$(UNITY_EDITOR_DIR)/Editor/Data/Managed/UnityEngine/UnityEngine.ImageConversionModule.dll"

PROJECT_DIR ?= C:/URack-Testbed
ASSEMBLY_DIR := $(PROJECT_DIR)/Library/ScriptAssemblies
PACKAGE_DLLS := $(wildcard $(ASSEMBLY_DIR)/*.dll)
EXCLUDE_DLLS := $(wildcard $(ASSEMBLY_DIR)/com.Eidetic.*.dll)
PACKAGE_DLLS := $(filter-out $(EXCLUDE_DLLS), $(PACKAGE_DLLS))

HARMONY_DLL := -r:Runtime/Plugins/Harmony/0Harmony.dll

# swap space delimeter to mcs lib argument
EMPTY :=
SPACE := $(EMPTY) $(EMPTY)
PACKAGE_DLLS := $(subst $(SPACE),$(EMPTY) -r:,$(PACKAGE_DLLS))

#change to the environment compiler
CSHARP_COMPILER = mcs

CSHARP_FLAGS = -t:library -out:URack-Unity.dll
CSHARP_SOURCE = -recurse:Utility/*.cs -recurse:Runtime/*.cs

all:
	@ $(CSHARP_COMPILER) $(CSHARP_FLAGS) \
		$(UNITYENGINE_DLLS) $(HARMONY_DLL)\
		-r:$(PACKAGE_DLLS) $(CSHARP_SOURCE)
