#include "pch-c.h"
#ifndef _MSC_VER
# include <alloca.h>
#else
# include <malloc.h>
#endif


#include "codegen/il2cpp-codegen-metadata.h"





// 0x00000001 System.Boolean Unity.Barracuda.BurstBLAS::IsNative()
extern void BurstBLAS_IsNative_m3287EC2915EE4B0A5A26B778413EBED790C2EEA9 (void);
// 0x00000002 System.Boolean Unity.Barracuda.BurstBLAS::IsCurrentPlatformSupported()
extern void BurstBLAS_IsCurrentPlatformSupported_m9EB46353878145AEE68463B84441CC5BB81949A5 (void);
// 0x00000003 System.Void Unity.Barracuda.BurstBLAS::SGEMM(System.Single*,System.Int32,System.Int32,System.Single*,System.Int32,System.Int32,System.Single*,System.Int32,System.Int32,System.Int32,System.Boolean,System.Boolean)
extern void BurstBLAS_SGEMM_m8C7AE0D5E0B47AEB8A5BFDF47B2DDB34940A2BDE (void);
// 0x00000004 Unity.Jobs.JobHandle Unity.Barracuda.BurstBLAS::ScheduleSGEMM(Unity.Jobs.JobHandle,System.Single*,System.Int32,System.Int32,System.Single*,System.Int32,System.Int32,System.Single*,System.Int32,System.Int32,System.Int32,System.Boolean,System.Boolean)
extern void BurstBLAS_ScheduleSGEMM_m3AE958567CBCE4EB983163893F28E01E966892DF (void);
// 0x00000005 System.Void Unity.Barracuda.BurstBLAS::.ctor()
extern void BurstBLAS__ctor_m0819B7915681A03944BB49E7B6B04EFC2F876151 (void);
static Il2CppMethodPointer s_methodPointers[5] = 
{
	BurstBLAS_IsNative_m3287EC2915EE4B0A5A26B778413EBED790C2EEA9,
	BurstBLAS_IsCurrentPlatformSupported_m9EB46353878145AEE68463B84441CC5BB81949A5,
	BurstBLAS_SGEMM_m8C7AE0D5E0B47AEB8A5BFDF47B2DDB34940A2BDE,
	BurstBLAS_ScheduleSGEMM_m3AE958567CBCE4EB983163893F28E01E966892DF,
	BurstBLAS__ctor_m0819B7915681A03944BB49E7B6B04EFC2F876151,
};
static const int32_t s_InvokerIndices[5] = 
{
	6351,
	6351,
	15,
	14,
	6559,
};
IL2CPP_EXTERN_C const Il2CppCodeGenModule g_Unity_Barracuda_BurstBLAS_CodeGenModule;
const Il2CppCodeGenModule g_Unity_Barracuda_BurstBLAS_CodeGenModule = 
{
	"Unity.Barracuda.BurstBLAS.dll",
	5,
	s_methodPointers,
	0,
	NULL,
	s_InvokerIndices,
	0,
	NULL,
	0,
	NULL,
	0,
	NULL,
	NULL,
	NULL, // module initializer,
	NULL,
	NULL,
	NULL,
};
