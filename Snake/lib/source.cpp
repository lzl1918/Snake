#define EXP_DLL extern "C" _declspec(dllexport)

enum DIRECTION
{
	LEFT, RIGHT, UP, DOWN, STOP
};
int turnx, turny;
extern "C" __declspec(dllexport) void init(int stageWidth, int stageHeight, int snakeX, int snakeY, int foodX, int foodY)
{
	turnx = foodX;
	turny = foodY;
}

extern "C" __declspec(dllexport) DIRECTION move(int snakeX, int snakeY, int foodX, int foodY)
{
	if (snakeX == foodX)
	{
		if (snakeY > foodY)
		{
			return UP;
		}
		else
		{
			return DOWN;
		}
	}
	else
	{
		if (snakeX > foodX)
			return LEFT;
		else
			return RIGHT;
	}
}
