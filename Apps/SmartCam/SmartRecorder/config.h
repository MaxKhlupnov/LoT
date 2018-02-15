#ifndef _CONFIG_H_
#define _CONFIG_H_

#include <ctype.h>
#include <iostream>
#include <fstream>
#include <string.h>
#include <vector>

using namespace std;

namespace SmartRecorder 
{

	class Config
	{
	public:
		Config();
		~Config();
		bool loadConf(int, int, char*);

		//Use improved GMM (Zivkovic et.al. '05) 
		int getN_Gaussian()			{return N_Gaussian;} // Number of Gaussians in GMM (max number)
		double getAlpha_LearningRate()	{return Alpha_LearningRate;} // learning rate for Gaussian Mixture Model
		double getInit_Var()		{return Init_Var;} // initial variance for a Gaussian in initial stage
		double getInit_new_Var()	{return Init_new_Var;} // initial variance for a new Gaussian in training and testing stage
		double getInit_new_w()	{return Init_new_w;} // initial weight for a new Gaussian in training and testing stage
		double getCT()			{return CT;} // CT value in GMM algorithm in (Zivkovic et.al. '05) 
		int getSE_Dilate1()	{return SE_Dilate1;} // size of the SE in dilation
		int getSE_Erode1()			{return SE_Erode1;} // size of the SE in erosion
		double getObjectDetectMinDelta()	{ return ObjectDetect_MinDelta; } // amount in percentage of the total image size (in pixels) that should have
																			  // atleast changed in the binary difference image (e.g. 5 % = 0.05)

	private:
		int ParametersNum;
		int FrameW;	
		int FrameH;

		int N_Gaussian;
		double Alpha_LearningRate;
		double Init_Var;
		double Init_new_Var;
		double Init_new_w;
		double CT;
		int SE_Dilate1;
		int SE_Erode1;
		double ObjectDetect_MinDelta;
	};
}

#endif