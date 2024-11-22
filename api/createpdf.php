<?php
	require('./fpdf.php');

	const CARD_WIDTH = 63.5;
	const CARD_HEIGHT = 88.9;

	$newPageInitialPositionX = 10;
	$newPageInitialPositionY = 10;

	$pdf = new FPDF();
	$pdf->AddPage();
	// TODO: function to determine next position on page 
	//
	$pdf->Image('https://cards.scryfall.io/png/front/9/4/94b2be42-467d-4210-b9bf-4d09ee504a22.png', $newPageInitialPositionX, $newPageInitialPositionY, CARD_WIDTH, CARD_HEIGHT);
	$pdf->Image('https://cards.scryfall.io/png/front/9/4/94b2be42-467d-4210-b9bf-4d09ee504a22.png', $newPageInitialPositionX + CARD_WIDTH, $newPageInitialPositionY, CARD_WIDTH, CARD_HEIGHT);
	$pdf->Image('https://cards.scryfall.io/png/front/9/4/94b2be42-467d-4210-b9bf-4d09ee504a22.png', $newPageInitialPositionX + CARD_WIDTH * 2, $newPageInitialPositionY, CARD_WIDTH, CARD_HEIGHT);
	
	$pdf->Image('https://cards.scryfall.io/png/front/9/4/94b2be42-467d-4210-b9bf-4d09ee504a22.png', $newPageInitialPositionX, $newPageInitialPositionY + CARD_HEIGHT, CARD_WIDTH, CARD_HEIGHT);
	$pdf->Image('https://cards.scryfall.io/png/front/9/4/94b2be42-467d-4210-b9bf-4d09ee504a22.png', $newPageInitialPositionX + CARD_WIDTH, $newPageInitialPositionY + CARD_HEIGHT, CARD_WIDTH, CARD_HEIGHT);
	$pdf->Image('https://cards.scryfall.io/png/front/9/4/94b2be42-467d-4210-b9bf-4d09ee504a22.png', $newPageInitialPositionX + CARD_WIDTH * 2, $newPageInitialPositionY + CARD_HEIGHT, CARD_WIDTH, CARD_HEIGHT);

	$pdf->Image('https://cards.scryfall.io/png/front/9/4/94b2be42-467d-4210-b9bf-4d09ee504a22.png', $newPageInitialPositionX, $newPageInitialPositionY + CARD_HEIGHT * 2, CARD_WIDTH, CARD_HEIGHT);
	$pdf->Image('https://cards.scryfall.io/png/front/9/4/94b2be42-467d-4210-b9bf-4d09ee504a22.png', $newPageInitialPositionX + CARD_WIDTH, $newPageInitialPositionY + CARD_HEIGHT * 2, CARD_WIDTH, CARD_HEIGHT);
	$pdf->Image('https://cards.scryfall.io/png/front/9/4/94b2be42-467d-4210-b9bf-4d09ee504a22.png', $newPageInitialPositionX + CARD_WIDTH * 2, $newPageInitialPositionY + CARD_HEIGHT * 2, CARD_WIDTH, CARD_HEIGHT);

	$pdf->AddPage();

    $pdf->Image('https://cards.scryfall.io/png/front/9/4/94b2be42-467d-4210-b9bf-4d09ee504a22.png', $newPageInitialPositionX, $newPageInitialPositionY, CARD_WIDTH, CARD_HEIGHT);
	$pdf->Image('https://cards.scryfall.io/png/front/9/4/94b2be42-467d-4210-b9bf-4d09ee504a22.png', $newPageInitialPositionX + CARD_WIDTH, $newPageInitialPositionY, CARD_WIDTH, CARD_HEIGHT);
	$pdf->Image('https://cards.scryfall.io/png/front/9/4/94b2be42-467d-4210-b9bf-4d09ee504a22.png', $newPageInitialPositionX + CARD_WIDTH * 2, $newPageInitialPositionY, CARD_WIDTH, CARD_HEIGHT);
	
	$pdf->Image('https://cards.scryfall.io/png/front/9/4/94b2be42-467d-4210-b9bf-4d09ee504a22.png', $newPageInitialPositionX, $newPageInitialPositionY + CARD_HEIGHT, CARD_WIDTH, CARD_HEIGHT);
	$pdf->Image('https://cards.scryfall.io/png/front/9/4/94b2be42-467d-4210-b9bf-4d09ee504a22.png', $newPageInitialPositionX + CARD_WIDTH, $newPageInitialPositionY + CARD_HEIGHT, CARD_WIDTH, CARD_HEIGHT);
	$pdf->Image('https://cards.scryfall.io/png/front/9/4/94b2be42-467d-4210-b9bf-4d09ee504a22.png', $newPageInitialPositionX + CARD_WIDTH * 2, $newPageInitialPositionY + CARD_HEIGHT, CARD_WIDTH, CARD_HEIGHT);

	$pdf->Image('https://cards.scryfall.io/png/front/9/4/94b2be42-467d-4210-b9bf-4d09ee504a22.png', $newPageInitialPositionX, $newPageInitialPositionY + CARD_HEIGHT * 2, CARD_WIDTH, CARD_HEIGHT);
	$pdf->Image('https://cards.scryfall.io/png/front/9/4/94b2be42-467d-4210-b9bf-4d09ee504a22.png', $newPageInitialPositionX + CARD_WIDTH, $newPageInitialPositionY + CARD_HEIGHT * 2, CARD_WIDTH, CARD_HEIGHT);
	$pdf->Image('https://cards.scryfall.io/png/front/9/4/94b2be42-467d-4210-b9bf-4d09ee504a22.png', $newPageInitialPositionX + CARD_WIDTH * 2, $newPageInitialPositionY + CARD_HEIGHT * 2, CARD_WIDTH, CARD_HEIGHT);

	$pdf->Output('D', 'booster.pdf');
?>