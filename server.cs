// Apartment ownership
// By: McTwist (9845)
// Makes it possible to give apartments to other players without giving special restrictions

// These are only dummies, to make sure someone doesn't use it badly
function fxDTSBrick::setApartment(%brick){}
function Apartment::setOwner(%this){}

// Set events
// Using Apartment object as output to make an another tag
registerInputEvent(fxDTSBrick, setApartment, "And Apartment");
registerOutputEvent(Apartment, setOwner, "int 0 999999 0");

// Get list of owners
function fxDTSBrick::getOwnerList(%brick)
{
	%owners = "";
	for (%i = 0; %i < %brick.numEvents; %i++)
	{
		if (%brick.eventInput[%i] !$= "setApartment" || %brick.eventOutput[%i] !$= "setOwner")
			continue;
		
		%ID = %brick.eventOutputParameter[%i, 1];
		
		// Validate
		if (!(%ID >= 0 || %ID <= 999999))
			continue;
		
		// Add to list
		%owners = %owners $= "" ? %ID : %owners SPC %id;
	}
	return %owners;
}
// Check if owner of the brick
function fxDTSBrick::isOwner(%brick, %bl_id)
{
	for (%i = 0; %i < %brick.numEvents; %i++)
	{
		if (%brick.eventInput[%i] !$= "setApartment" || %brick.eventOutput[%i] !$= "setOwner")
			continue;
		
		%ID = %brick.eventOutputParameter[%i, 1];
		
		// Validate
		if (!(%ID >= 0 || %ID <= 999999))
			continue;
		
		// Found it
		if (%ID $= %bl_id)
			return true;
	}
	return false;
}
// Get first owner
function fxDTSBrick::getFirstOwner(%brick)
{
	for (%i = 0; %i < %brick.numEvents; %i++)
	{
		if (%brick.eventInput[%i] !$= "setApartment" || %brick.eventOutput[%i] !$= "setOwner")
			continue;
		
		%ID = %brick.eventOutputParameter[%i, 1];
		
		// Validate
		if (!(%ID >= 0 || %ID <= 999999))
			continue;
		
		return %ID;
	}
	return -1;
}

// The real "magic"
package PackageApartment
{
	// When the brick is loaded
	function fxDTSBrick::onLoadPlant(%brick)
	{
		Parent::onLoadPlant(%brick);
		
		%owner = -1;
		// Check bricks down
		for (%i = 0; %owner $= -1 && %i < %brick.getNumDownBricks(); %i++)
		{
			%next = %brick.getDownBrick(%i);
			%owner = %next.getFirstOwner();
		}
		// Check bricks up
		//for (%i = 0; %owner $= -1 && %i < %brick.getNumUpBricks(); %i++)
		//{
		//	%next = %brick.getUpBrick(%i);
		//	%owner = %next.getFirstOwner();
		//}
		// Found real owner
		if (%owner !$= -1)
		{
			%group = "BrickGroup_" @ %owner;
			// Create group
			if (!isObject(%group))
			{
				new SimGroup(%group)
				{
					bl_id = %owner;
					name = "\c1BL_ID: " @ %owner @ "\c1\c0";
					client = 0;
				};
				mainBrickGroup.add(%group);
			}
			// This is critical
			%group.schedule(0, add, %brick);
		}
	}
	
	// Checking trust level
	function getTrustLevel(%obj1, %obj2)
	{
		// Check the bricks
		if (isObject(%obj1) && %obj1.getType() & $TypeMasks::FxBrickAlwaysObjectType
			&& isObject(%obj2) && %obj2.getType() & $TypeMasks::FxBrickAlwaysObjectType)
		{
			// Is a renter
			if (%obj1.isOwner(getBrickGroupFromObject(%obj2).bl_id))
			{
				return 3;
			}

			// Check trust level
			%owners = %obj1.getOwnerList();
			%count = getWordCount(%owners);
			for (%i = 0; %i < %count; %i++)
			{
				%client = findClientByBL_ID(getWord(%owners, %i));
				%trust = getTrustLevel(%client, %obj2);
				if (%trust >= 1)
					return %trust;
			}
		}
		return Parent::getTrustLevel(%obj1, %obj2);
	}
};
activatePackage(PackageApartment);
