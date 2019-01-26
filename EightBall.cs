using System;

public class EightBall
{
    private readonly string[] Answers =
    {
        "No, this cannot be so.",
        "Perhaps this could happen.",
        "You may never know the answer.",
        "If you were to know, your retinas would burn.",
        "If I told you, I'd have to kill you.",
        "Yes, this will occur.",
        "This will never happen, stop dreaming.",
        "If you try hard enough, it may be so.",
        "What do you think I am, an oracle?",
    };

	public EightBall()
	{

	}

    public int RandomNumber()
    {
        int rn = new Random().Next(0, 7);
        return rn;
    }

    public string Choose(int index)
    {
        return Answers[index];
    }
}