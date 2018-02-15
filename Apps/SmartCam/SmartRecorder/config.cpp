#include "stdafx.h"
#include "config.h"

namespace SmartRecorder
{
	Config::Config()
	{

		ParametersNum = 9;

		// Default Values
		N_Gaussian = 2;
		Alpha_LearningRate = 0.9;
		Init_Var = 500;
		Init_new_Var = 500;
		Init_new_w = 0.15;
		CT = 0.01;
		SE_Dilate1 = 2;
		SE_Erode1 = 5;
		ObjectDetect_MinDelta = 0.01;
	}

	Config::~Config()
	{
	}

	bool Config::loadConf(int wid, int hei, char* parameterload)
	{
		FrameW = wid;
		FrameH = hei;

		ifstream infile;
		infile.open(parameterload);				// open a file

		if(!infile.good())								// if there is no such file
		{
			cout << "[Error]: ConfLoader::loadConf(): open config file" << endl;
			return false;
		}

		char line[256];									// temp register for char
		char *pch;
		int count = 0;

		while(!infile.eof())							// if not end of file
		{
			infile.getline(line, 256);				// get the whole line into line[256]
			if(line[0] == '#')						// if this line starts with #, it is just a comment line
			{
				// comment line
				continue;
			}

			pch = strtok(line, " =");				// extract the char except " " and "="  
			while(pch != NULL)										
			{
				// N_Gaussian
				if(strcmp(pch, "N_Gaussian") == 0)		// if pch equals to N_Gaussian
				{
					pch = strtok(NULL, " =");
					N_Gaussian = atoi(pch);
					count = count + 1;					 
					break;
				}
				// Alpha_LearningRate
				else if(strcmp(pch, "Alpha_LearningRate") == 0)		// if pch equals to Alpha_LearningRate
				{
					pch = strtok(NULL, " =");
					Alpha_LearningRate = atof(pch);
					count = count + 1;					 
					break;
				}
				// Init_Var
				else if(strcmp(pch, "Init_Var") == 0)		// if pch equals to Init_Var
				{
					pch = strtok(NULL, " =");
					Init_Var = atof(pch);
					count = count + 1;					 
					break;
				}
				// Init_new_Var
				else if(strcmp(pch, "Init_new_Var") == 0)		// if pch equals to Init_new_Var
				{
					pch = strtok(NULL, " =");
					Init_new_Var = atof(pch);
					count = count + 1;					 
					break;
				}
				//Init_new_w
				else if(strcmp(pch, "Init_new_w") == 0)		// if pch equals to Init_new_w
				{
					pch = strtok(NULL, " =");
					Init_new_w = atof(pch);
					count = count + 1;					 
					break;
				}
				//CT
				else if(strcmp(pch, "CT") == 0)		// if pch equals to CT
				{
					pch = strtok(NULL, " =");
					CT = atof(pch);
					count = count + 1;					 
					break;
				}
				// SE_Dilate1
				else if(strcmp(pch, "SE_Dilate1") == 0)		// if pch equals to SE_Dilate1
				{
					pch = strtok(NULL, " =");
					SE_Dilate1 = atoi(pch);
					count = count + 1;					 
					break;
				}
				// SE_Erode1
				else if(strcmp(pch, "SE_Erode1") == 0)		// if pch equals to SE_Erode1
				{
					pch = strtok(NULL, " =");
					SE_Erode1 = atoi(pch);
					count = count + 1;					 
					break;
				}
				// ObjectDetect_MinDelta
				else if(strcmp(pch, "ObjectDetect_MinDelta") == 0)		// if pch equals to ObjectDetect_MinDelta
				{
					pch = strtok(NULL, " =");
					ObjectDetect_MinDelta = atof(pch);
					count = count + 1;					 
					break;
				}
				else
				{
					cout << "Error]: ConfLoader::loadConf(): There is no parameter " << pch << "in the tracker.conf" << endl;
					return false;
				}
			}
		}

		infile.close();

		if(count != ParametersNum) 
		{
			cout << "[Error]: ConfLoader::loadConf(): count(" << count <<") != " << ParametersNum << endl;
			return false;
		}

		return true;
	}
}
