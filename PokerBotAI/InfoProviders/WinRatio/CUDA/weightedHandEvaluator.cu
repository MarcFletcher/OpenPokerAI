#ifndef _HANDEVALUATOR_KERNEL_H_
#define _HANDEVALUATOR_KERNEL_H_

#define BLOCK_DIM 8
#define NUM_RANDS 25
#define NUM_CARDS 52

extern "C" __global__ void Evaluate_Hand_Weighted(int *handEvalData, float *randoms, int hand1, int hand2, int table1, int table2, int table3, int table4, int table5, int numberPlayers, int *totalResult)
{
	__shared__ char randomNumbers[BLOCK_DIM][BLOCK_DIM][NUM_CARDS];
	
	unsigned int xIndex = blockIdx.x * blockDim.x + threadIdx.x;
	unsigned int yIndex = blockIdx.y * blockDim.y + threadIdx.y;
	
	char temp = (char)0;
	int index = 0;
	int numberHighCardsAvailable = 0;

	for(int i=0; i <= 51; i++)
	{
		if(hand1 != (i + 1) && hand2 != (i + 1) && table1 != (i + 1) && table2 != (i + 1) && table3 != (i + 1) && table4 != (i + 1) && table5 != (i + 1))
		{
			randomNumbers[threadIdx.x][threadIdx.y][index] = (char)(i);

			if(i >= 32)
				numberHighCardsAvailable++;

			index++;
		}
	}

	int numberHighCardsRequired = 2 * ( numberPlayers - 1);
	int numberLowCardsRequired =  index - 45;

	if(numberHighCardsRequired > numberHighCardsAvailable)
	{
		numberLowCardsRequired = numberLowCardsRequired + numberHighCardsRequired - numberHighCardsAvailable;
		numberHighCardsRequired = numberHighCardsAvailable;
	}

	int numberCardsInDeck = index;

	for(int i = numberHighCardsAvailable - 1; i >= numberHighCardsAvailable - numberHighCardsRequired; i--)
	{
		index = (int)(numberCardsInDeck - numberHighCardsAvailable + randoms[(yIndex * gridDim.x * blockDim.x + xIndex) * NUM_RANDS + (NUM_CARDS - 1 - i)] * (i + 1));
		temp = randomNumbers[threadIdx.x][threadIdx.y][numberCardsInDeck - 1 - (numberHighCardsAvailable - 1 - i)];
		randomNumbers[threadIdx.x][threadIdx.y][numberCardsInDeck - 1 - (numberHighCardsAvailable - 1 - i)] = randomNumbers[threadIdx.x][threadIdx.y][index];
		randomNumbers[threadIdx.x][threadIdx.y][index] = temp;
	}	

	for(int i = numberCardsInDeck - numberHighCardsRequired - 1; i >= numberCardsInDeck - numberHighCardsRequired - numberLowCardsRequired; i--)
	{
		index = (int)(randoms[(yIndex * gridDim.x * blockDim.x + xIndex) * NUM_RANDS + (NUM_CARDS - 1 - i)] * (i + 1));
		temp = randomNumbers[threadIdx.x][threadIdx.y][i];
		randomNumbers[threadIdx.x][threadIdx.y][i] = randomNumbers[threadIdx.x][threadIdx.y][index];
		randomNumbers[threadIdx.x][threadIdx.y][index] = temp;
	}

	__syncthreads();
	
	unsigned int randomIndex = 0;
	/*	
	if(table1 == 0)
	{
		table1 = randomNumbers[threadIdx.x][threadIdx.y][numberCardsInDeck - 2 * (numberPlayers - 1) - randomIndex - 1] + 1;			
		randomIndex++;
	}
	if(table2 == 0)
	{
		table2 = randomNumbers[threadIdx.x][threadIdx.y][numberCardsInDeck - 2 * (numberPlayers - 1) - randomIndex - 1] + 1;			
		randomIndex++;
	}
	if(table3 == 0)
	{
		table3 = randomNumbers[threadIdx.x][threadIdx.y][numberCardsInDeck - 2 * (numberPlayers - 1) - randomIndex - 1] + 1;			
		randomIndex++;
	}*/
	if(table4 == 0)
	{
		table4 = randomNumbers[threadIdx.x][threadIdx.y][numberCardsInDeck - 2 * (numberPlayers - 1) - randomIndex - 1] + 1;			
		randomIndex++;
	}
	if(table5 == 0)
	{
		table5 = randomNumbers[threadIdx.x][threadIdx.y][numberCardsInDeck - 2 * (numberPlayers - 1) - randomIndex - 1] + 1;			
		randomIndex++;
	}
	
	int tableIndex = handEvalData[53 + table1];
	tableIndex = handEvalData[tableIndex + table2];
	tableIndex = handEvalData[tableIndex + table3];
	tableIndex = handEvalData[tableIndex + table4];
	tableIndex = handEvalData[tableIndex + table5];
	
	int myHandValue = handEvalData[tableIndex + hand1];
	myHandValue = handEvalData[myHandValue + hand2];
	
	int opponentHole1;
	int opponentHole2;

	int opponentHandValue;
	int result = 1;
	randomIndex = 0;

	for(int i=1;i<numberPlayers;i++)
	{
		opponentHole1 = randomNumbers[threadIdx.x][threadIdx.y][numberCardsInDeck - randomIndex - 1] + 1;			
		randomIndex++;

		opponentHole2 = randomNumbers[threadIdx.x][threadIdx.y][numberCardsInDeck - randomIndex - 1] + 1;			
		randomIndex++;
		
		opponentHandValue = handEvalData[tableIndex + opponentHole1];
		opponentHandValue = handEvalData[opponentHandValue + opponentHole2];
		
		if(opponentHandValue > myHandValue)
		{
			result = -1;	
			break;
		}
	}
	
	__syncthreads();

	totalResult[(gridDim.x * blockIdx.y + blockIdx.x) * blockDim.x * blockDim.y + blockDim.x * threadIdx.y + threadIdx.x] = result;

	//totalResult[yIndex * gridDim.x * blockDim.x + xIndex ] = result;
}

#endif // _HANDEVALUATOR_KERNEL_H_
