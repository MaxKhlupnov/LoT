#include "stdafx.h"
#include "ObjectDetector.h"

#define GEN_RANDOM_OBJECT 0

using namespace std;
using namespace System;
using namespace System::Runtime::InteropServices;

namespace SmartRecorder
{


bool compareWoverStd(GMM n1, GMM n2)  //call by TrackList::sortListByPosY()    near ----> far
{
	return (n1.getWeight()/sqrt(n1.getVar())) > (n2.getWeight()/sqrt(n2.getVar()));
}


GMM::GMM()
{
	Mu = 0;
	Var = 0;
	Weight = 1;
}


GMM::~GMM()
{

}

GMM::GMM(double a, double b, double c)
{
	Mu = a;
	Var = b;
	Weight = c;
}


ObjectDetector::ObjectDetector()
{
	FrameW = 0;
	FrameH = 0;
	Dest = NULL;
	Para = NULL;
	BgModelR = NULL;
	BgModelG = NULL;
	BgModelB = NULL;
}


ObjectDetector::~ObjectDetector()
{
	delete [] Dest;
	delete Para;

	delete [] BgModelR;
	delete [] BgModelG;
	delete [] BgModelB;
}


// check if the detector has been initialized
bool ObjectDetector::IsInitialized()
{
	return FrameH != 0 && FrameW != 0;
}

// initialization of the object detector
bool ObjectDetector::InitializeFromFrame(byte *d, int cbytes, int frameWidth, int frameHeight, char *configFile)
{
	FrameW = frameWidth;
	FrameH = frameHeight;

	if (NULL == Dest)
	{
		Dest = new byte[frameWidth * frameHeight]; // binary
	}

	if (NULL == Para)
	{
		Para = new Config();
	}

	OnlineInitialBgModel(d, frameWidth, frameHeight, configFile);

	return true;
}

System::Drawing::Rectangle ObjectDetector::GetObjectRectRandom()
{
	System::Drawing::Rectangle rectObject;
	Random^ randomGen = gcnew Random(GetTickCount());
	rectObject.X = 0;
	rectObject.Y = 0;
	rectObject.Width = 0;
	rectObject.Height = 0;

	if (randomGen->Next() % 13 == 0 && randomGen->Next() % 13 == 0)
	{
		rectObject.X = FrameW / ((randomGen->Next() % 8) + 1);
		rectObject.Y = FrameH / ((randomGen->Next() % 8) + 1);
		rectObject.Width = FrameW / ((randomGen->Next() % 2) + 2);
		if (rectObject.X + rectObject.Width > FrameW)
		{
			rectObject.X = FrameW - rectObject.Width;
		}
		rectObject.Height = FrameH / ((randomGen->Next() % 2) + 2);
		if (rectObject.Y + rectObject.Height > FrameH)
		{
			rectObject.Y = FrameH - rectObject.Height;
		}
	}

	return rectObject;
}

System::Drawing::Rectangle ObjectDetector::ExtractObjectRectFromBinaryImage(byte *d, int frameWidth, int frameHeight)
{
	int x0 = -1;
	int y0 = -1;
	int x1 = -1;
	int y1 = -1;
	int counter = 0;
	System::Drawing::Rectangle rectObject;
	rectObject.X = 0;
	rectObject.Y = 0;
	rectObject.Width = 0;
	rectObject.Height = 0;

	for (int x = 0; x < frameWidth; ++x)
	{
		for (int y = 0; y < frameHeight; ++y)
		{
			if (d[x+y*frameWidth] == 255)
			{
				if (x0 == -1 || x0 > x) x0 = x;
				if (x1 == -1 || x1 < x) x1 = x;
				if (y0 == -1 || y0 > y) y0 = y;
				if (y1 == -1 || y1 < y) y1 = y;

				counter++;
			}
		}
	}

	if ((x0 != -1 && x1 != -1 && y0 != -1 && y1 != -1) &&
		(x1 > x0 && y1 > y0) && (counter > (int)(Para->getObjectDetectMinDelta() * frameWidth * frameHeight)))
	{
		rectObject.X = x0;
		rectObject.Y = y0;
		rectObject.Width = x1 - x0;
		rectObject.Height = y1 - y0;
	}

	return rectObject;
}

void ObjectDetector::GetBinaryImage(byte *d)
{
	memcpy(d, Dest, FrameW * FrameH);
}

System::Drawing::Rectangle ObjectDetector::GetObjectRect(byte *d, int cbytes)
{
	System::Drawing::Rectangle rectObject;
	rectObject.X = 0;
	rectObject.Y = 0;
	rectObject.Width = 0;
	rectObject.Height = 0;

	static int throttle = 0;

	if (++throttle % 1 == 0)
	{
		OnlineBuildBgModel(d);
		BgSubtraction(d, Dest);
#if GEN_RANDOM_OBJECT
		rectObject=GetObjectRectRandom();
#else
		rectObject = ExtractObjectRectFromBinaryImage(Dest, FrameW, FrameH);
#endif
	}
	return rectObject;
}

// should only be called once during initialization
void ObjectDetector::OnlineInitialBgModel(unsigned char* d, int w, int h, char* parameterload)
{
	if (NULL != parameterload)
	{
		Para->loadConf(w, h, parameterload);			// load the parameters
	}

	FrameW = w;		
	FrameH = h;

	if (NULL != BgModelR)
	{
		delete [] BgModelR;
		BgModelR = NULL;
	}
	BgModelR = new vector<GMM>[FrameW*FrameH];

	if (NULL != BgModelG)
	{
		delete [] BgModelG;
		BgModelG = NULL;
	}
	BgModelG = new vector<GMM>[FrameW*FrameH];

	if (NULL != BgModelB)
	{
		delete [] BgModelB;
		BgModelB = NULL;
	}
	BgModelB = new vector<GMM>[FrameW*FrameH];
	
	NGaussian = Para->getN_Gaussian();		
	int step = 255/(NGaussian+2);
	int index;
	double dataR, dataG, dataB;

	// always start from single Gaussian
	for(int y=0; y<FrameH; ++y)		// for each pixel
	{
		for(int x=0; x<FrameW; ++x)
		{
			index = y*FrameW+x;
			dataR = d[3*index+2];		dataG = d[3*index+1];		dataB = d[3*index];
			for(int g=0; g<NGaussian; ++g)					// for each gaussian
			{					
				GMM tempR(dataR, Para->getInit_Var(), 1.0/NGaussian);		// use the first frame to initialize 
				GMM tempG(dataG, Para->getInit_Var(), 1.0/NGaussian); 
				GMM tempB(dataB, Para->getInit_Var(), 1.0/NGaussian); 

				BgModelR[index].push_back(tempR);	BgModelG[index].push_back(tempG);	BgModelB[index].push_back(tempB);
			}

		}
	}	
}

void ObjectDetector::OnlineBuildBgModel(unsigned char* d)
{
	int index;
	//double sum_wR, sum_wG, sum_wB;
	double dataR, dataG, dataB;
	//double totalprobR, totalprobG, totalprobB;
	double rhoR, rhoG, rhoB;
	double sumwR, sumwG, sumwB;

	bool* matchR = new bool [NGaussian];		bool* matchG = new bool [NGaussian];		bool* matchB = new bool [NGaussian];
	bool matchindexR = 0;		bool matchindexG = 0;		bool matchindexB = 0;
	double* probR = new double [NGaussian];		double* probG = new double [NGaussian];		double* probB = new double [NGaussian]; 

	//unsigned char* intermediate1 = new unsigned char[FrameH*FrameW];		// binary
	int GaussianR, GaussianG, GaussianB;			// number of Gausian for each channel of each pixel

	for(int y=0; y<FrameH; ++y)		// for each pixel
	{	
		for(int x=0; x<FrameW; ++x)
		{
			index = y*FrameW+x;	
			matchindexR = 0;
			matchindexG = 0;
			matchindexB = 0;
			GaussianR = (int) BgModelR[index].size();		GaussianG = (int) BgModelG[index].size();		GaussianB = (int) BgModelB[index].size();
			//========================== update the background model ==========================
			///////sort the Gaussians based on the weights

			//// since the number of Gaussian is small, just use bubble sort is fast enough
			for(int j=GaussianR-1; j>0; --j)
			{
				for(int k=0; k<j; ++k)
				{
					if( (BgModelR[index][k].getWeight()) < (BgModelR[index][k+1].getWeight()) )
						swap(BgModelR[index][k], BgModelR[index][k+1]);						
				}
			}
			for(int j=GaussianG-1; j>0; --j)
			{
				for(int k=0; k<j; ++k)
				{
					if( (BgModelG[index][k].getWeight()) < (BgModelG[index][k+1].getWeight()) )
						swap(BgModelG[index][k], BgModelG[index][k+1]);
				}
			}
			for(int j=GaussianB-1; j>0; --j)
			{
				for(int k=0; k<j; ++k)
				{
					if( (BgModelB[index][k].getWeight()) < (BgModelB[index][k+1].getWeight()) )
						swap(BgModelB[index][k], BgModelB[index][k+1]);
				}
			}

			//========================== update whole GMM model ==========================
			dataR = d[3*index+2];		dataG = d[3*index+1];		dataB = d[3*index];

			for(int j=0; j<GaussianR; ++j)
			{
				probR[j] = (BgModelR[index])[j].getWeight()*(1/(sqrt(2*PI_OBJECTDETECTION*(BgModelR[index])[j].getVar())))*exp(-0.5*(dataR-(BgModelR[index])[j].getMu())*(dataR-(BgModelR[index])[j].getMu())/((BgModelR[index])[j].getVar()));
			}
			for(int j=0; j<GaussianG; ++j)
			{
				probG[j] = (BgModelG[index])[j].getWeight()*(1/(sqrt(2*PI_OBJECTDETECTION*(BgModelG[index])[j].getVar())))*exp(-0.5*(dataG-(BgModelG[index])[j].getMu())*(dataG-(BgModelG[index])[j].getMu())/((BgModelG[index])[j].getVar()));
			}
			for(int j=0; j<GaussianB; ++j)
			{
				probB[j] = (BgModelB[index])[j].getWeight()*(1/(sqrt(2*PI_OBJECTDETECTION*(BgModelB[index])[j].getVar())))*exp(-0.5*(dataB-(BgModelB[index])[j].getMu())*(dataB-(BgModelB[index])[j].getMu())/((BgModelB[index])[j].getVar()));
			}

			for(int j=0; j<GaussianR; ++j)
			{
				// R
				if( (matchindexR == 0)  && abs(dataR-(BgModelR[index])[j].getMu()) <= 2.5*sqrt((BgModelR[index])[j].getVar()))		// no others match before and match this 
				{
					matchR[j] = 1;
					matchindexR = 1;
				}
				else
					matchR[j] = 0;
			}
			for(int j=0; j<GaussianG; ++j)
			{
				// G
				if( (matchindexG == 0)  && abs(dataG-(BgModelG[index])[j].getMu()) <= 2.5*sqrt((BgModelG[index])[j].getVar()))		// no others match before and match this 
				{
					matchG[j] = 1;
					matchindexG = 1;
				}
				else
					matchG[j] = 0;
			}
			for(int j=0; j<GaussianB; ++j)
			{
				// B
				if( (matchindexB == 0)  && abs(dataB-(BgModelB[index])[j].getMu()) <= 2.5*sqrt((BgModelB[index])[j].getVar()))		// no others match before and match this 
				{
					matchB[j] = 1;
					matchindexB = 1;
				}
				else
					matchB[j] = 0;
			}

			if(NGaussian>1)						// if more than one Gaussians, follow the paper
			{
				/// R
				sumwR = 0;
				if(matchindexR == 1)				// at least one match is found
				{
					for(int j=0; j<GaussianR; ++j)
					{
						///// update weight (matched and unmatched are all updated)
						double tempweight = (1-Para->getAlpha_LearningRate())*(BgModelR[index])[j].getWeight()+Para->getAlpha_LearningRate()*matchindexR-Para->getAlpha_LearningRate()*Para->getCT();
						if(tempweight>0)
							BgModelR[index][j].setWeight(tempweight);
						else
							BgModelR[index][j].setWeight(0);
						
						if(matchR[j] == 1)			// only mean and var of mached ones are updated
						{
							///// update mean
							rhoR = Para->getAlpha_LearningRate()/(BgModelR[index])[j].getWeight();
							(BgModelR[index])[j].setMu((1-rhoR)*(BgModelR[index])[j].getMu()+rhoR*dataR);

							///// update var
							(BgModelR[index])[j].setVar((1-rhoR)*(BgModelR[index])[j].getVar()+rhoR*(dataR-(BgModelR[index])[j].getMu())*(dataR-(BgModelR[index])[j].getMu()));
						}


						sumwR = sumwR + (BgModelR[index])[j].getWeight();			// accumulate the w
					}
				}
				else			// No match is found
				{
					if(GaussianR == NGaussian)				// if the number of Gaussian attains max
					{
						//// The lease probable one (smallest weight) is replaced with the new data
						BgModelR[index][GaussianR-1].setWeight(Para->getInit_new_w());				// assign the initial weight a small value (or we can just remain the value? since it is already the smallest one)
						(BgModelR[index])[GaussianR-1].setMu(dataR);
						(BgModelR[index])[GaussianR-1].setVar(Para->getInit_new_Var());			// assign a high initial variance

						sumwR = sumwR + (BgModelR[index])[GaussianR-1].getWeight();			// accumulate the w

						//// the remaining Gaussians (from index 0 ~ NGaussian-2)
						for(int j=0; j<GaussianR-1; ++j)		
						{
							///// update weight (as the unmatched updated)
							double tempweight = (1-Para->getAlpha_LearningRate())*(BgModelR[index])[j].getWeight()-Para->getAlpha_LearningRate()*Para->getCT();
							if(tempweight>0)
								BgModelR[index][j].setWeight(tempweight);
							else
								BgModelR[index][j].setWeight(0);
							
							sumwR = sumwR + (BgModelR[index])[j].getWeight();			// accumulate the w
							///// the mean and var remain the same
						}
					}
					else				// if the number of Gaussian does not attain max yet
					{
						GMM tempR(dataR, Para->getInit_new_Var(), Para->getInit_new_w());
						(BgModelR[index]).push_back(tempR);			// so the number of Gaussian needs to be updated
						GaussianR = GaussianR + 1;

						sumwR = sumwR + (BgModelR[index])[GaussianR-1].getWeight();			// accumulate the w

						//// the remaining Gaussians (from index 0 ~ NGaussian-2)
						for(int j=0; j<GaussianR-1; ++j)		
						{
							///// update weight (as the unmatched updated)
							double tempweight = (1-Para->getAlpha_LearningRate())*(BgModelR[index])[j].getWeight()-Para->getAlpha_LearningRate()*Para->getCT();
							if(tempweight>0)
								BgModelR[index][j].setWeight(tempweight);
							else
								BgModelR[index][j].setWeight(0);
							
							sumwR = sumwR + (BgModelR[index])[j].getWeight();			// accumulate the w
							///// the mean and var remain the same
						}

					}
				}

				/// G
				sumwG = 0;
				if(matchindexG == 1)				// at least one match is found
				{
					for(int j=0; j<GaussianG; ++j)
					{
						///// update weight (matched and unmatched are all updated)
						double tempweight = (1-Para->getAlpha_LearningRate())*(BgModelG[index])[j].getWeight()+Para->getAlpha_LearningRate()*matchindexG-Para->getAlpha_LearningRate()*Para->getCT();
						if(tempweight>0)
							BgModelG[index][j].setWeight(tempweight);
						else
							BgModelG[index][j].setWeight(0);
						
						if(matchG[j] == 1)			// only mean and var of mached ones are updated
						{
							///// update mean
							rhoG = Para->getAlpha_LearningRate()/(BgModelG[index])[j].getWeight();
							(BgModelG[index])[j].setMu((1-rhoG)*(BgModelG[index])[j].getMu()+rhoG*dataG);

							///// update var
							(BgModelG[index])[j].setVar((1-rhoG)*(BgModelG[index])[j].getVar()+rhoG*(dataG-(BgModelG[index])[j].getMu())*(dataG-(BgModelG[index])[j].getMu()));
						}


						sumwG = sumwG + (BgModelG[index])[j].getWeight();			// accumulate the w
					}
				}
				else			// No match is found
				{
					if(GaussianG == NGaussian)				// if the number of Gaussian attains max
					{
						//// The lease probable one (smallest weight) is replaced with the new data
						BgModelG[index][GaussianG-1].setWeight(Para->getInit_new_w());				// assign the initial weight a small value (or we can just remain the value? since it is already the smallest one)
						(BgModelG[index])[GaussianG-1].setMu(dataG);
						(BgModelG[index])[GaussianG-1].setVar(Para->getInit_new_Var());			// assign a high initial variance

						sumwG = sumwG + (BgModelG[index])[GaussianG-1].getWeight();			// accumulate the w

						//// the remaining Gaussians (from index 0 ~ NGaussian-2)
						for(int j=0; j<GaussianG-1; ++j)		
						{
							///// update weight (as the unmatched updated)
							double tempweight = (1-Para->getAlpha_LearningRate())*(BgModelG[index])[j].getWeight()-Para->getAlpha_LearningRate()*Para->getCT();
							if(tempweight>0)
								BgModelG[index][j].setWeight(tempweight);
							else
								BgModelG[index][j].setWeight(0);
							
							sumwG = sumwG + (BgModelG[index])[j].getWeight();			// accumulate the w
							///// the mean and var remain the same
						}
					}
					else				// if the number of Gaussian does not attain max yet
					{
						GMM tempG(dataG, Para->getInit_new_Var(), Para->getInit_new_w());
						(BgModelG[index]).push_back(tempG);			// so the number of Gaussian needs to be updated
						GaussianG = GaussianG + 1;

						sumwG = sumwG + (BgModelG[index])[GaussianG-1].getWeight();			// accumulate the w

						//// the remaining Gaussians (from index 0 ~ NGaussian-2)
						for(int j=0; j<GaussianG-1; ++j)		
						{
							///// update weight (as the unmatched updated)
							double tempweight = (1-Para->getAlpha_LearningRate())*(BgModelG[index])[j].getWeight()-Para->getAlpha_LearningRate()*Para->getCT();
							if(tempweight>0)
								BgModelG[index][j].setWeight(tempweight);
							else
								BgModelG[index][j].setWeight(0);
							
							sumwG = sumwG + (BgModelG[index])[j].getWeight();			// accumulate the w
							///// the mean and var remain the same
						}

					}
				}

				/// B
				sumwB = 0;
				if(matchindexB == 1)				// at least one match is found
				{
					for(int j=0; j<GaussianB; ++j)
					{
						///// update weight (matched and unmatched are all updated)
						double tempweight = (1-Para->getAlpha_LearningRate())*(BgModelB[index])[j].getWeight()+Para->getAlpha_LearningRate()*matchindexB-Para->getAlpha_LearningRate()*Para->getCT();
						if(tempweight>0)
							BgModelB[index][j].setWeight(tempweight);
						else
							BgModelB[index][j].setWeight(0);
						
						if(matchB[j] == 1)			// only mean and var of mached ones are updated
						{
							///// update mean
							rhoB = Para->getAlpha_LearningRate()/(BgModelB[index])[j].getWeight();
							(BgModelB[index])[j].setMu((1-rhoB)*(BgModelB[index])[j].getMu()+rhoB*dataB);

							///// update var
							(BgModelB[index])[j].setVar((1-rhoB)*(BgModelB[index])[j].getVar()+rhoB*(dataB-(BgModelB[index])[j].getMu())*(dataB-(BgModelB[index])[j].getMu()));
						}


						sumwB = sumwB + (BgModelB[index])[j].getWeight();			// accumulate the w
					}
				}
				else			// No match is found
				{
					if(GaussianB == NGaussian)				// if the number of Gaussian attains max
					{
						//// The lease probable one (smallest weight) is replaced with the new data
						BgModelB[index][GaussianB-1].setWeight(Para->getInit_new_w());				// assign the initial weight a small value (or we can just remain the value? since it is already the smallest one)
						(BgModelB[index])[GaussianB-1].setMu(dataB);
						(BgModelB[index])[GaussianB-1].setVar(Para->getInit_new_Var());			// assign a high initial variance

						sumwB = sumwB + (BgModelB[index])[GaussianB-1].getWeight();			// accumulate the w

						//// the remaining Gaussians (from index 0 ~ NGaussian-2)
						for(int j=0; j<GaussianB-1; ++j)		
						{
							///// update weight (as the unmatched updated)
							double tempweight = (1-Para->getAlpha_LearningRate())*(BgModelB[index])[j].getWeight()-Para->getAlpha_LearningRate()*Para->getCT();
							if(tempweight>0)
								BgModelB[index][j].setWeight(tempweight);
							else
								BgModelB[index][j].setWeight(0);
							
							sumwB = sumwB + (BgModelB[index])[j].getWeight();			// accumulate the w
							///// the mean and var remain the same
						}
					}
					else				// if the number of Gaussian does not attain max yet
					{
						GMM tempB(dataB, Para->getInit_new_Var(), Para->getInit_new_w());
						(BgModelB[index]).push_back(tempB);			// so the number of Gaussian needs to be updated
						GaussianB = GaussianB + 1;

						sumwB = sumwB + (BgModelB[index])[GaussianB-1].getWeight();			// accumulate the w

						//// the remaining Gaussians (from index 0 ~ NGaussian-2)
						for(int j=0; j<GaussianB-1; ++j)		
						{
							///// update weight (as the unmatched updated)
							double tempweight = (1-Para->getAlpha_LearningRate())*(BgModelB[index])[j].getWeight()-Para->getAlpha_LearningRate()*Para->getCT();
							if(tempweight>0)
								BgModelB[index][j].setWeight(tempweight);
							else
								BgModelB[index][j].setWeight(0);
							
							sumwB = sumwB + (BgModelB[index])[j].getWeight();			// accumulate the w
							///// the mean and var remain the same
						}

					}
				}
			}
			else						// if maximum NGaussian == 1, always do the update
			{
				/// R
				sumwR = 0;
				for(int j=0; j<GaussianR; ++j)
				{
					///// update weight
					BgModelR[index][j].setWeight(1);

					///// update mean
					rhoR = Para->getAlpha_LearningRate();
					(BgModelR[index])[j].setMu((1-rhoR)*(BgModelR[index])[j].getMu()+rhoR*dataR);

					///// update var
					(BgModelR[index])[j].setVar((1-rhoR)*(BgModelR[index])[j].getVar()+rhoR*(dataR-(BgModelR[index])[j].getMu())*(dataR-(BgModelR[index])[j].getMu()));														

					sumwR = 1;			// accumulate the w
				}

				/// G
				sumwG = 0;
				for(int j=0; j<GaussianG; ++j)
				{
					///// update weight
					BgModelG[index][j].setWeight(1);

					///// update mean
					rhoG = Para->getAlpha_LearningRate();
					(BgModelG[index])[j].setMu((1-rhoG)*(BgModelG[index])[j].getMu()+rhoG*dataG);

					///// update var
					(BgModelG[index])[j].setVar((1-rhoG)*(BgModelG[index])[j].getVar()+rhoG*(dataG-(BgModelG[index])[j].getMu())*(dataG-(BgModelG[index])[j].getMu()));														

					sumwG = 1;			// accumulate the w
				}

				/// B
				sumwB = 0;
				for(int j=0; j<GaussianB; ++j)
				{
					///// update weight
					BgModelB[index][j].setWeight(1);

					///// update mean
					rhoB = Para->getAlpha_LearningRate();
					(BgModelB[index])[j].setMu((1-rhoB)*(BgModelB[index])[j].getMu()+rhoB*dataB);

					///// update var
					(BgModelB[index])[j].setVar((1-rhoB)*(BgModelB[index])[j].getVar()+rhoB*(dataB-(BgModelB[index])[j].getMu())*(dataB-(BgModelB[index])[j].getMu()));														

					sumwB = 1;			// accumulate the w
				}
			}


			for(int j=0; j<GaussianR; ++j)			// re-normalize w
			{
				BgModelR[index][j].setWeight((BgModelR[index])[j].getWeight()/sumwR);
			}
			for(int j=0; j<GaussianG; ++j)			// re-normalize w
			{
				BgModelG[index][j].setWeight((BgModelG[index])[j].getWeight()/sumwG);
			}
			for(int j=0; j<GaussianB; ++j)			// re-normalize w
			{
				BgModelB[index][j].setWeight((BgModelB[index])[j].getWeight()/sumwB);
			}

		}
	}

	delete [] probB;
	delete [] probG;
	delete [] probR;
	delete [] matchB;
	delete [] matchG;
	delete [] matchR;

}

void ObjectDetector::BgSubtraction(unsigned char* d, unsigned char* dest)
{
	const double default_th = 20;
	int index;
	double dataR, dataG, dataB;
	//double diffR, diffG, diffB;
	double ThR = default_th;
	double ThG = default_th;
	double ThB = default_th;

	unsigned char* intermediate1 = new unsigned char[FrameH*FrameW];		// binary
	unsigned char* diffR = new unsigned char[FrameH*FrameW];		// binary
	unsigned char* diffG = new unsigned char[FrameH*FrameW];		// binary
	unsigned char* diffB = new unsigned char[FrameH*FrameW];		// binary
	int foreg = 0;

	for(int y=0; y<FrameH; ++y)		// for each pixel
	{	
		for(int x=0; x<FrameW; ++x)
		{
			index = y*FrameW+x;	
			dataR = d[3*index+2];		dataG = d[3*index+1];		dataB = d[3*index];

			diffR[index] = (byte) abs(dataR-(BgModelR[index])[0].getMu());
			diffG[index] = (byte) abs(dataG-(BgModelG[index])[0].getMu());
			diffB[index] = (byte) abs(dataB-(BgModelB[index])[0].getMu());		

			(BgModelR[index])[0].setMu((1-Para->getAlpha_LearningRate())*(BgModelR[index])[0].getMu() + Para->getAlpha_LearningRate()*dataR);			// moving average update background
			(BgModelG[index])[0].setMu((1-Para->getAlpha_LearningRate())*(BgModelG[index])[0].getMu() + Para->getAlpha_LearningRate()*dataG);
			(BgModelB[index])[0].setMu((1-Para->getAlpha_LearningRate())*(BgModelB[index])[0].getMu() + Para->getAlpha_LearningRate()*dataB);
		}
	}

	ThR = Otsu_Threshold(diffR, default_th);//		ThR = max(ThR, default_th);				// set the lower bound
	ThG = Otsu_Threshold(diffG, default_th);//		ThG = max(ThG, default_th);
	ThB = Otsu_Threshold(diffB, default_th);//		ThB = max(ThB, default_th);
	//cout<<" ThR = "<<ThR<<" ThG = "<<ThG<<" ThB = "<<ThB<<endl;				// [debug]

	for(int y=0; y<FrameH; ++y)		// for each pixel
	{	
		for(int x=0; x<FrameW; ++x)
		{
			foreg = 0 ;
			index = y*FrameW+x;	

			if(diffR[index] >= ThR)
				foreg++;
			if(diffG[index] >= ThG)
				foreg++;
			if(diffB[index] >= ThB)
				foreg++;

			if(foreg>=1)
				intermediate1[index] = 255;
			else
				intermediate1[index] = 0;				
		}
	}

	PostProcessing(intermediate1, dest);

	delete [] intermediate1;

	delete [] diffB;
	delete [] diffG;
	delete [] diffR;
}

int ObjectDetector::Otsu_Threshold(unsigned char* absdiff, double lower_bound)
{
	double* hist = new double [256];
	for (int i = 0; i<256; i++)
		hist[i] = 0;

	int pixelindex;
	for (int i =0;i<FrameH;i++)
	{
		for (int j =0;j<FrameW;j++)
		{	
			pixelindex = i*FrameW+j;
			hist[(int) absdiff[pixelindex]]++;
		}
	}

	double w1, w2, u1, u2, utotal, sigma_b, u1_temp, u2_temp;
  	double max = -1;
	int index = (int) lower_bound;
	utotal = 0;
	for (int i = 0; i<256; i++)
	{
		hist[i] = hist[i]/(FrameH*FrameW);			// normalization
		utotal = utotal + i*hist[i];				// global mean for the histogram
	}
	w1 = hist[0];
	u2_temp = 0;
	u1_temp = 0;			// 0*hist[0] = 0;

	for (int i=1;i< (int)lower_bound;i++)
	{	
		w1 = w1 + hist[i];
		w2 = 1 -w1;

		u1_temp = u1_temp + i*hist[i];
		u2_temp = utotal - u1_temp;			
	}
	for (int i= (int)lower_bound;i<=255;i++)
	{	
		w1 = w1 + hist[i];
		w2 = 1 -w1;

		u1_temp = u1_temp + i*hist[i];
		u2_temp = utotal - u1_temp;	
		u1 = u1_temp/w1;
		u2 = u2_temp/w2;

		sigma_b = w1*w2*(u1-u2)*(u1-u2);
		if (sigma_b > max)
		{
			max = sigma_b;
			index  = i;
		}
	}

	delete [] hist;
	return index;
}


void ObjectDetector::Dilation(unsigned char* ori, int size, unsigned char* dest)
{
	bool dilation_index = 0;
	int size_half, y_valid, x_valid;

	size_half = (size-1)/2;

	for(int y=0; y<FrameH; ++y)		// for each center location of structure element
	{	
		for(int x=0; x<FrameW; ++x)
		{
			dilation_index = 0;
			for(int dy = -size_half; dy<=size_half; ++dy)
			{
				for(int dx = -size_half; dx<=size_half; ++dx)
				{
					if(y+dy < 0)					// get the valid coordinate; consider the border by using reflection
						y_valid = -1-(y+dy);
					else if(y+dy>=FrameH)
						y_valid = 2*FrameH-1-(y+dy);
					else
						y_valid = y+dy;

					if(x+dx < 0)					// get the valid coordinate; consider the border by using reflection
						x_valid = -1-(x+dx);
					else if(x+dx>=FrameW)
						x_valid = 2*FrameW-1-(x+dx);
					else
						x_valid = x+dx;

					if(ori[y_valid*FrameW+x_valid] == 255)
					{
						dilation_index = 1;
						break;										// at least one pixel of se lies in the foreground	
					}
				}
				if(dilation_index == 1)								// at least one pixel of se lies in the foreground
				{
					break;
				}
			}
			if(dilation_index == 1)									// at least one pixel of se lies in the foreground
				dest[y*FrameW+x] = 255;
			else
				dest[y*FrameW+x] = 0;
		}
	}
}

void ObjectDetector::Erosion(unsigned char* ori, int size, unsigned char* dest)
{
	int erosion_index = 0;
	int size_half, y_valid, x_valid;

	size_half = (size-1)/2;

	for(int y=0; y<FrameH; ++y)		// for each center location of structure element
	{	
		for(int x=0; x<FrameW; ++x)
		{
			erosion_index = 0;
			for(int dy = -size_half; dy<=size_half; ++dy)
			{
				for(int dx = -size_half; dx<=size_half; ++dx)
				{
					if(y+dy < 0)					// get the valid coordinate; consider the border by using reflection
						y_valid = -1-(y+dy);
					else if(y+dy>=FrameH)
						y_valid = 2*FrameH-1-(y+dy);
					else
						y_valid = y+dy;

					if(x+dx < 0)					// get the valid coordinate; consider the border by using reflection
						x_valid = -1-(x+dx);
					else if(x+dx>=FrameW)
						x_valid = 2*FrameW-1-(x+dx);
					else
						x_valid = x+dx;

					if(ori[y_valid*FrameW+x_valid] == 255)
					{
						erosion_index++;
					}
				}
			}
			if(erosion_index == (2*size_half+1)*(2*size_half+1))			// all the se must be in te forground
				dest[y*FrameW+x] = 255;
			else
				dest[y*FrameW+x] = 0;
		}
	}
}

void ObjectDetector::PostProcessing(unsigned char* intermediate, unsigned char* dest)
{
	
	unsigned char* intermediate2 = new unsigned char[FrameH*FrameW];		// binary
	Dilation(intermediate, Para->getSE_Dilate1(), intermediate2);
	Erosion(intermediate2, Para->getSE_Erode1(), dest);

	delete [] intermediate2;
}

}