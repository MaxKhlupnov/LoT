#ifndef _OBJECTDETECTION_H
#define _OBJECTDETECTION_H

#include <stdio.h>
#include <iostream>
#include <assert.h>
#include <vector>
#include <algorithm>	//for sort
#include <math.h>	//for sort
#include <ctime>
#include "config.h"

using namespace std;

#define PI_OBJECTDETECTION	3.1415926			//	pi

namespace SmartRecorder
{

class GMM
{
public:
	GMM();
	GMM(double, double, double );
	~GMM();

	double getMu()	{return Mu;}
	double getVar()	{return Var;}
	double getWeight()	{return Weight;}

	void setMu(double x)	{Mu = x;}
	void setVar(double x)	{Var = x;}
	void setWeight(double x)	{Weight = x;}
	
private:
	double Mu;		// the mean value for each Gaussian
	double Var;		// the var value for each Gaussian
	double Weight;	// the weight value for each Gaussian	
};



public ref class ObjectDetector
{
public:
	ObjectDetector();
	~ObjectDetector();

	bool InitializeFromFrame(byte *d, int cbytes, int frameWidth, int frameHeight, char *configFile);
	bool IsInitialized();
	System::Drawing::Rectangle GetObjectRect(byte *d, int cbytes);


	void ObjectDetector::GetBinaryImage(byte *d);

private:
	
	void OnlineInitialBgModel(unsigned char* , int, int, char*);				// generate the initial bgmodel
	void OnlineBuildBgModel(unsigned char* d);				// learn the model online
	void BgSubtraction(unsigned char* d, unsigned char* dest);
	
	int Otsu_Threshold(unsigned char* absdiff, double lower_bound);
	void PostProcessing(unsigned char* d, unsigned char* dest);				// postprocessing after background subtraction
	void Erosion(unsigned char* ori, int size, unsigned char* dest);
	void Dilation(unsigned char* ori, int size, unsigned char* dest);

	System::Drawing::Rectangle GetObjectRectRandom();
	System::Drawing::Rectangle ExtractObjectRectFromBinaryImage(byte *d, int frameWidth, int frameHeight);
	int FrameW;
	int FrameH;
	int NGaussian;				// number of Gaussian (normally 3~5 is enough)
	byte* Dest;

	vector<GMM>*	BgModelR;
	vector<GMM>*	BgModelG;
	vector<GMM>*	BgModelB;

	Config *Para;

};

}
#endif