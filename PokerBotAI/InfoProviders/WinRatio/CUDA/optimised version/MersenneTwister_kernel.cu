//THIS IS CURRENTLY BROKEN DO NOT USE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!


/*
 * Copyright 1993-2009 NVIDIA Corporation.  All rights reserved.
 *
 * NVIDIA Corporation and its licensors retain all intellectual property and 
 * proprietary rights in and to this software and related documentation and 
 * any modifications thereto.  Any use, reproduction, disclosure, or distribution 
 * of this software and related documentation without an express license 
 * agreement from NVIDIA Corporation is strictly prohibited.
 * 
 */

#define  DCMT_SEED 4172
#define  MT_RNG_PERIOD 607


typedef struct{
    unsigned int matrix_a;
    unsigned int mask_b;
    unsigned int mask_c;
    unsigned int seed;
} mt_struct_stripped;


#define   MT_RNG_COUNT 4096
#define   MT_MM 9
#define   MT_NN 19
#define   MT_WMASK 0xFFFFFFFFU
#define   MT_UMASK 0xFFFFFFFEU
#define   MT_LMASK 0x1U
#define   MT_SHIFT0 12
#define   MT_SHIFTB 7
#define   MT_SHIFTC 15
#define   MT_SHIFT1 18

//__device__ static mt_struct_stripped ds_MT[MT_RNG_COUNT];

////////////////////////////////////////////////////////////////////////////////
// Write MT_RNG_COUNT vertical lanes of NPerRng random numbers to *d_Random.
// For coalesced global writes MT_RNG_COUNT should be a multiple of warp size.
// Initial states for each generator are the same, since the states are
// initialized from the global seed. In order to improve distribution properties
// on small NPerRng supply dedicated (local) seed to each twister.
// The local seeds, in their turn, can be extracted from global seed
// by means of any simple random number generator, like LCG.
////////////////////////////////////////////////////////////////////////////////
extern "C" __global__ void RandomGPU(
    float *d_Random,    
	char *ds_MT_Bytes,
	unsigned int *seeds,
	int NPerRng	
){
    const int      tid = blockDim.x * blockIdx.x + threadIdx.x;   
	const int seedCount = (blockDim.x * (blockIdx.x + 1)) / MT_RNG_COUNT;
	
	mt_struct_stripped *ds_MT = (mt_struct_stripped*)ds_MT_Bytes;
	mt_struct_stripped config = ds_MT[tid - seedCount * MT_RNG_COUNT];
	
	unsigned int matrix_a = config.matrix_a;
	unsigned int mask_b = config.mask_b;
	unsigned int mask_c = config.mask_c;

    int iState, iState1, iStateM, iOut;
    unsigned int mti, mti1, mtiM, x;
    unsigned int mt[MT_NN];

    
    mt[0] = seeds[seedCount];
    for(iState = 1; iState < MT_NN; iState++)
        mt[iState] = (1812433253U * (mt[iState - 1] ^ (mt[iState - 1] >> 30)) + iState) & MT_WMASK;

    iState = 0;
    mti1 = mt[0];
    for(iOut = 0; iOut < NPerRng; iOut++){
        iState1 = iState + 1;
        iStateM = iState + MT_MM;
        if(iState1 >= MT_NN) iState1 -= MT_NN;
        if(iStateM >= MT_NN) iStateM -= MT_NN;
        mti  = mti1;
        mti1 = mt[iState1];
        mtiM = mt[iStateM];

        x    = (mti & MT_UMASK) | (mti1 & MT_LMASK);
        x    =  mtiM ^ (x >> 1) ^ ((x & 1) ? matrix_a : 0);
        mt[iState] = x;
        iState = iState1;

        
        x ^= (x >> MT_SHIFT0);
        x ^= (x << MT_SHIFTB) & mask_b;
        x ^= (x << MT_SHIFTC) & mask_c;
        x ^= (x >> MT_SHIFT1);
        
        d_Random[tid + iOut * gridDim.x * blockDim.x] = ((float)x + 1.0f) / 4294967296.0f;		
	}
}
